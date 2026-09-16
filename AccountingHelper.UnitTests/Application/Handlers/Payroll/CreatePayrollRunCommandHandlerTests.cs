using AccountingHelper.Application.Exceptions;
using AccountingHelper.Application.Features.Payroll.Commands.CreatePayrollRun;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Exceptions;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;

namespace AccountingHelper.UnitTests.Application.Handlers.Payroll;

public class CreatePayrollRunCommandHandlerTests
{
    // A distinct token (never CancellationToken.None) so every Setup/Verify below
    // proves the handler forwards the caller's token down each awaited call.
    private static readonly CancellationToken Ct = new CancellationTokenSource().Token;

    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPayrollRunRepository> _payrollRunRepositoryMock;
    private readonly Mock<ILogger<CreatePayrollRunCommandHandler>> _loggerMock;

    // Nothing is ever verified on this mock. Publishing is transport behaviour, covered against
    // a real broker in the integration suite; asserting it here would only restate the handler.
    private readonly Mock<IPublishEndpoint> _publishEndpointMock;
    private readonly CreatePayrollRunCommandHandler _handler;

    public CreatePayrollRunCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _payrollRunRepositoryMock = new Mock<IPayrollRunRepository>();
        _loggerMock = new Mock<ILogger<CreatePayrollRunCommandHandler>>();
        _publishEndpointMock = new Mock<IPublishEndpoint>();

        _unitOfWorkMock.Setup(u => u.PayrollRuns).Returns(_payrollRunRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new CreatePayrollRunCommandHandler(
            _unitOfWorkMock.Object,
            _publishEndpointMock.Object,
            _loggerMock.Object);
    }

    private static CreatePayrollRunCommand ValidCommand(int year = 2026, int month = 1) =>
        new(Year: year, Month: month);

    [Fact]
    public async Task Handle_WhenPeriodIsFree_CreatesPendingRun()
    {
        // ARRANGE
        _payrollRunRepositoryMock
            .Setup(r => r.ExistsForPeriodAsync(2026, 1, Ct))
            .ReturnsAsync(false);

        PayrollRun? added = null;
        _payrollRunRepositoryMock
            .Setup(r => r.Add(It.IsAny<PayrollRun>()))
            .Callback<PayrollRun>(run => added = run);

        // ACT
        var response = await _handler.Handle(ValidCommand(), Ct);

        // ASSERT
        // A run starts as Pending with an id assigned by the handler: nothing has been computed
        // yet, and the consumer needs that id to exist before the message goes out.
        added.Should().NotBeNull();
        added!.Id.Should().NotBeEmpty();
        added.Status.Should().Be(PayrollRunStatus.Pending);
        added.Year.Should().Be(2026);
        added.Month.Should().Be(1);

        response.Id.Should().Be(added.Id);
        response.Status.Should().Be(PayrollRunStatus.Pending);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(Ct), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRunForPeriodExists_ThrowsConflict_AndDoesNotSave()
    {
        // ARRANGE
        _payrollRunRepositoryMock
            .Setup(r => r.ExistsForPeriodAsync(2026, 1, Ct))
            .ReturnsAsync(true);

        // ACT
        var act = () => _handler.Handle(ValidCommand(), Ct);

        // ASSERT
        await act.Should().ThrowAsync<ConflictException>();

        _payrollRunRepositoryMock.Verify(r => r.Add(It.IsAny<PayrollRun>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUniqueIndexRejectsTheInsert_ThrowsConflict()
    {
        // ARRANGE
        // The race the courtesy check cannot cover: ExistsForPeriodAsync says the period is free,
        // and between that answer and the insert another request takes it. Without the translation
        // the client would get a 500 for a situation the API already has a code for.
        _payrollRunRepositoryMock
            .Setup(r => r.ExistsForPeriodAsync(2026, 1, Ct))
            .ReturnsAsync(false);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UniqueConstraintViolationException(
                "ix_payroll_runs_year_month",
                new InvalidOperationException()));

        // ACT
        var act = () => _handler.Handle(ValidCommand(), Ct);

        // ASSERT
        await act.Should().ThrowAsync<ConflictException>();
    }
}
