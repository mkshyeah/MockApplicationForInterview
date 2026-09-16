using AccountingHelper.Application.Exceptions;
using AccountingHelper.Application.Features.Payroll.Commands.RunPayroll;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Exceptions;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;

namespace AccountingHelper.UnitTests.Application.Handlers.Payroll;

public class RunPayrollCommandHandlerTests
{
    // A distinct token (never CancellationToken.None) so every Setup/Verify below
    // proves the handler forwards the caller's token down each awaited call.
    private static readonly CancellationToken Ct = new CancellationTokenSource().Token;

    private static readonly Guid RunId = Guid.NewGuid();

    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IPayrollRunRepository> _payrollRunRepositoryMock = new();
    private readonly Mock<IPayslipRepository> _payslipRepositoryMock = new();
    private readonly Mock<IEmployeeRepository> _employeeRepositoryMock = new();
    private readonly Mock<IPublishEndpoint> _publishEndpointMock = new();

    private readonly RunPayrollCommandHandler _handler;

    private List<Payslip> _addedPayslips = [];

    public RunPayrollCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.PayrollRuns).Returns(_payrollRunRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Payslips).Returns(_payslipRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Employees).Returns(_employeeRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _payslipRepositoryMock
            .Setup(r => r.AddRange(It.IsAny<IEnumerable<Payslip>>()))
            .Callback<IEnumerable<Payslip>>(payslips => _addedPayslips = payslips.ToList());

        _handler = new RunPayrollCommandHandler(
            _unitOfWorkMock.Object,
            _publishEndpointMock.Object,
            Mock.Of<ILogger<RunPayrollCommandHandler>>());
    }

    private void RunExists(PayrollRunStatus status = PayrollRunStatus.Pending) =>
        _payrollRunRepositoryMock
            .Setup(r => r.GetByIdAsync(RunId, Ct))
            .ReturnsAsync(new PayrollRun
            {
                Id = RunId,
                Year = 2026,
                Month = 1,
                Status = status
            });

    private void EmployeesAre(params PayrollEmployeeSnapshot[] snapshots) =>
        _employeeRepositoryMock
            .Setup(r => r.GetPayrollEmployeeSnapshotsAsync(Ct))
            .ReturnsAsync(snapshots);

    [Fact]
    public async Task Handle_WhenRunDoesNotExist_ThrowsNotFound()
    {
        // ARRANGE
        _payrollRunRepositoryMock
            .Setup(r => r.GetByIdAsync(RunId, Ct))
            .ReturnsAsync((PayrollRun?)null);

        // ACT
        var act = () => _handler.Handle(new RunPayrollCommand(RunId), Ct);

        // ASSERT
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ForEmployeeWithSalary_WritesGrossTaxAndNet()
    {
        // ARRANGE
        var employeeId = Guid.NewGuid();
        RunExists();
        EmployeesAre(new PayrollEmployeeSnapshot(employeeId, 1_000m, SalaryType.Monthly));

        // ACT
        await _handler.Handle(new RunPayrollCommand(RunId), Ct);

        // ASSERT
        // 1000 monthly sits in the lowest bracket, so tax is 10% and net is the remainder.
        // The numbers come from SalaryCalculator, not from a second formula written here.
        var payslip = _addedPayslips.Should().ContainSingle().Subject;

        payslip.EmployeeId.Should().Be(employeeId);
        payslip.PayrollRunId.Should().Be(RunId);
        payslip.Gross.Should().Be(1_000.00m);
        payslip.Tax.Should().Be(100.00m);
        payslip.Deductions.Should().Be(0m);
        payslip.Net.Should().Be(900.00m);
    }

    [Fact]
    public async Task Handle_ForHourlySalary_ConvertsToMonthlyBeforePaying()
    {
        // ARRANGE
        // An hourly rate must not land in the payslip as-is: the run pays a month, and the
        // conversion is the domain rule that makes the two comparable.
        RunExists();
        EmployeesAre(new PayrollEmployeeSnapshot(Guid.NewGuid(), 600m, SalaryType.Hourly));

        // ACT
        await _handler.Handle(new RunPayrollCommand(RunId), Ct);

        // ASSERT
        // 600/hour * 2080 hours / 12 months = 104 000 per month.
        _addedPayslips.Should().ContainSingle().Subject.Gross.Should().Be(104_000.00m);
    }

    [Fact]
    public async Task Handle_ForEmployeeWithoutActiveSalary_SkipsWithoutFailingTheRun()
    {
        // ARRANGE
        var paid = Guid.NewGuid();
        RunExists();
        EmployeesAre(
            new PayrollEmployeeSnapshot(paid, 1_000m, SalaryType.Monthly),
            new PayrollEmployeeSnapshot(Guid.NewGuid(), null, null));

        PayrollRun? completed = null;
        _payrollRunRepositoryMock
            .Setup(r => r.ApplyProgressAsync(It.IsAny<PayrollRun>(), Ct))
            .Callback<PayrollRun, CancellationToken>((run, _) =>
            {
                if (run.Status == PayrollRunStatus.Completed) completed = run;
            });

        // ACT
        await _handler.Handle(new RunPayrollCommand(RunId), Ct);

        // ASSERT
        // One broken row must not cost everyone else their payslip, so the run completes and
        // says how many it walked past instead of failing.
        _addedPayslips.Should().ContainSingle().Subject.EmployeeId.Should().Be(paid);

        completed.Should().NotBeNull();
        completed!.ProcessedCount.Should().Be(1);
        completed.SkippedCount.Should().Be(1);
        completed.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WhenRunIsAlreadyCompleted_DoesNothing()
    {
        // ARRANGE
        RunExists(PayrollRunStatus.Completed);

        // ACT
        await _handler.Handle(new RunPayrollCommand(RunId), Ct);

        // ASSERT
        // The cheap path for a redelivered message: no recompute, no write, no second event.
        _employeeRepositoryMock.Verify(
            r => r.GetPayrollEmployeeSnapshotsAsync(It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _publishEndpointMock.Verify(
            p => p.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUniqueIndexRejectsPayslips_AcknowledgesInsteadOfThrowing()
    {
        // ARRANGE
        // A concurrent delivery beat us to it. The work exists, so this is success: throwing would
        // retry and eventually dead-letter a run that actually finished.
        RunExists();
        EmployeesAre(new PayrollEmployeeSnapshot(Guid.NewGuid(), 1_000m, SalaryType.Monthly));

        var saves = 0;
        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(() => ++saves == 1
                ? Task.CompletedTask
                : Task.FromException(new UniqueConstraintViolationException(
                    "ix_payslips_payroll_run_id_employee_id",
                    new InvalidOperationException())));

        // ACT
        var act = () => _handler.Handle(new RunPayrollCommand(RunId), Ct);

        // ASSERT
        await act.Should().NotThrowAsync();

        // and no completion event, because this delivery did not complete anything
        _publishEndpointMock.Verify(
            p => p.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
