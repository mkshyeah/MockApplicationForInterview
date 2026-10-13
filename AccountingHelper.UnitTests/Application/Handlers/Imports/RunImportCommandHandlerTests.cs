using System.Text;
using AccountingHelper.Application.Features.Employees.Commands.CreateEmployee;
using AccountingHelper.Application.Features.Imports;
using AccountingHelper.Application.Features.Imports.Commands.RunImport;
using AccountingHelper.Contracts.Messaging;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Exceptions;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace AccountingHelper.UnitTests.Application.Handlers.Imports;

// The happy path, row errors and Failed are covered against a real database in ImportRunExecutionTests.
// What lives here are the races: a unique violation at exactly the wrong moment cannot be produced on
// demand in an integration test, but a mock can throw it on the second save every time.
public class RunImportCommandHandlerTests
{
    // A distinct token (never CancellationToken.None) so every Setup below proves the handler forwards
    // the caller's token down each awaited call.
    private static readonly CancellationToken Ct = new CancellationTokenSource().Token;

    private static readonly Guid JobId = Guid.NewGuid();

    private const string Csv =
        "FirstName,LastName,Email,Department,Position,SalaryAmount,SalaryType,HireDate\n" +
        "Aigerim,Nurlanova,raced@test.com,Engineering,Developer,1000,Monthly,2024-01-15\n" +
        "Daniyar,Akhmetov,free@test.com,Engineering,Developer,1000,Monthly,2024-01-15\n";

    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IImportJobRepository> _importJobRepositoryMock = new();
    private readonly Mock<IEmployeeRepository> _employeeRepositoryMock = new();
    private readonly Mock<IPublishEndpoint> _publishEndpointMock = new();

    private readonly RunImportCommandHandler _handler;

    // Snapshots of the job as each ApplyProgressAsync saw it: the job is one mutable object, so only a copy
    // taken at call time shows what a particular save would have written.
    private readonly List<(ImportJobStatus Status, int Imported, int Failed)> _progress = [];
    private readonly List<ImportError> _addedErrors = [];
    private int _employeesAdded;

    public RunImportCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.ImportJobs).Returns(_importJobRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Employees).Returns(_employeeRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.LeaveBalances).Returns(Mock.Of<ILeaveBalanceRepository>());

        var departments = new Mock<IDepartmentRepository>();
        departments.Setup(r => r.GetAllAsync(Ct))
            .ReturnsAsync([new Department { Id = Guid.NewGuid(), Name = "Engineering" }]);
        _unitOfWorkMock.Setup(u => u.Departments).Returns(departments.Object);

        var positions = new Mock<IPositionRepository>();
        positions.Setup(r => r.GetAllAsync(Ct))
            .ReturnsAsync([new Position { Id = Guid.NewGuid(), Title = "Developer", Grade = EmployeeGrade.Junior }]);
        _unitOfWorkMock.Setup(u => u.Positions).Returns(positions.Object);

        _importJobRepositoryMock
            .Setup(r => r.GetByIdAsync(JobId, Ct))
            .ReturnsAsync(new ImportJob { Id = JobId, FileName = "employees.csv", Status = ImportJobStatus.Pending });

        _importJobRepositoryMock
            .Setup(r => r.GetFileAsync(JobId, Ct))
            .ReturnsAsync(new ImportFile { ImportJobId = JobId, ContentType = "text/csv", Content = Encoding.UTF8.GetBytes(Csv) });

        _importJobRepositoryMock
            .Setup(r => r.ApplyProgressAsync(It.IsAny<ImportJob>(), Ct))
            .Callback<ImportJob, CancellationToken>((job, _) => _progress.Add((job.Status, job.ImportedCount, job.FailedCount)))
            .Returns(Task.CompletedTask);

        _importJobRepositoryMock
            .Setup(r => r.AddErrors(It.IsAny<IEnumerable<ImportError>>()))
            .Callback<IEnumerable<ImportError>>(errors => _addedErrors.AddRange(errors));

        _employeeRepositoryMock
            .Setup(r => r.Add(It.IsAny<Employee>()))
            .Callback(() => _employeesAdded++);

        _handler = new RunImportCommandHandler(
            _unitOfWorkMock.Object,
            _publishEndpointMock.Object,
            Options.Create(new ImportSettings()),
            new CreateEmployeeCommandValidator(),
            Mock.Of<ILogger<RunImportCommandHandler>>());
    }

    private static UniqueConstraintViolationException Violation(string constraint) =>
        new(constraint, new Exception("23505"));

    // Saves in order: 1 = job to Processing, 2.. = batch attempts, last = completion.
    private void SavesAre(params Func<Task>[] saves)
    {
        var call = 0;
        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(Ct))
            .Returns(() => call < saves.Length ? saves[call++]() : Task.CompletedTask);
    }

    private static Task Ok() => Task.CompletedTask;

    private static Func<Task> Throws(string constraint) => () => Task.FromException(Violation(constraint));

    [Fact]
    public async Task Handle_WhenBatchLosesEmailRace_RestagesIt_AndReportsTheTakenEmail()
    {
        // ARRANGE
        // First lookup: nobody exists. Between it and the insert someone hires raced@test.com, so the save
        // fails, and the second lookup — the one the restage runs — sees the winner.
        _employeeRepositoryMock
            .SetupSequence(r => r.GetExistingEmailsAsync(It.IsAny<IReadOnlyCollection<string>>(), Ct))
            .ReturnsAsync(new HashSet<string>())
            .ReturnsAsync(new HashSet<string> { "raced@test.com" });

        SavesAre(Ok, Throws(DatabaseConstraints.EmployeeEmail), Ok, Ok);

        // ACT
        await _handler.Handle(new RunImportCommand(JobId), Ct);

        // ASSERT
        // 1 + 1, not 3 + 1: the counters of the failed attempt are rolled back before the restage adds its own.
        _progress.Should().Equal(
            (ImportJobStatus.Processing, 0, 0),
            (ImportJobStatus.Processing, 2, 0),
            (ImportJobStatus.Processing, 1, 1),
            (ImportJobStatus.Completed, 1, 1));

        _addedErrors.Should().ContainSingle()
            .Which.Message.Should().Contain("raced@test.com").And.Contain("already exists");

        // Two employees staged by the first attempt, one by the restage.
        _employeesAdded.Should().Be(3);

        // Once after the failed save — before restaging, or the rolled-back rows would ride along — and once
        // after the successful one.
        _unitOfWorkMock.Verify(u => u.ClearTracking(), Times.Exactly(2));

        _publishEndpointMock.Verify(
            p => p.Publish(It.Is<ImportCompleted>(m => m.ImportedCount == 1 && m.FailedCount == 1), Ct),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEmailRaceIsLostOnEveryAttempt_GivesUpToTheRetryPolicy()
    {
        // ARRANGE
        _employeeRepositoryMock
            .Setup(r => r.GetExistingEmailsAsync(It.IsAny<IReadOnlyCollection<string>>(), Ct))
            .ReturnsAsync(new HashSet<string>());

        var email = Throws(DatabaseConstraints.EmployeeEmail);
        SavesAre(Ok, email, email, email, email);

        // ACT
        var act = () => _handler.Handle(new RunImportCommand(JobId), Ct);

        // ASSERT
        // Bounded on purpose: three attempts here, then the exception reaches the consumer's retry policy
        // instead of a loop that could spin for as long as the contention lasts.
        await act.Should().ThrowAsync<UniqueConstraintViolationException>();

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(Ct), Times.Exactly(4));
        _progress.Should().NotContain(p => p.Status == ImportJobStatus.Completed);
    }

    [Fact]
    public async Task Handle_WhenAnotherDeliveryOwnsTheErrorRows_StopsWithoutCompletingTheJob()
    {
        // ARRANGE
        _employeeRepositoryMock
            .Setup(r => r.GetExistingEmailsAsync(It.IsAny<IReadOnlyCollection<string>>(), Ct))
            .ReturnsAsync(new HashSet<string>());

        SavesAre(Ok, Throws(DatabaseConstraints.ImportErrorRow));

        // ACT
        var act = () => _handler.Handle(new RunImportCommand(JobId), Ct);

        // ASSERT
        // Returns, so the message is acknowledged: the other delivery is doing the work. Completing the job
        // from here would publish counters that are not final.
        await act.Should().NotThrowAsync();

        _progress.Should().NotContain(p => p.Status == ImportJobStatus.Completed);
        _publishEndpointMock.Verify(p => p.Publish(It.IsAny<ImportCompleted>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAnotherDeliveryAlreadyFailedTheJob_StopsQuietly()
    {
        // ARRANGE
        _importJobRepositoryMock
            .Setup(r => r.GetFileAsync(JobId, Ct))
            .ReturnsAsync(new ImportFile { ImportJobId = JobId, ContentType = "text/csv", Content = Encoding.UTF8.GetBytes("FirstName\nA\n") });

        SavesAre(Throws(DatabaseConstraints.ImportErrorRow));

        // ACT
        var act = () => _handler.Handle(new RunImportCommand(JobId), Ct);

        // ASSERT
        // Both deliveries reached the same verdict; the second one writing it again is not an error.
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Handle_WhenAnUnexpectedConstraintIsViolated_Rethrows()
    {
        // ARRANGE
        _employeeRepositoryMock
            .Setup(r => r.GetExistingEmailsAsync(It.IsAny<IReadOnlyCollection<string>>(), Ct))
            .ReturnsAsync(new HashSet<string>());

        SavesAre(Ok, Throws("ix_something_else"));

        // ACT
        var act = () => _handler.Handle(new RunImportCommand(JobId), Ct);

        // ASSERT
        // Only the two indexes with a known meaning are handled; anything else is a bug and must be loud.
        (await act.Should().ThrowAsync<UniqueConstraintViolationException>())
            .Which.ConstraintName.Should().Be("ix_something_else");

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(Ct), Times.Exactly(2));
    }
}
