using AccountingHelper.Application.Exceptions;
using AccountingHelper.Application.Features.LeaveRequests.Commands.SubmitLeaveRequest;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace AccountingHelper.UnitTests.Application.Handlers.LeaveRequests;

public class SubmitLeaveRequestCommandHandlerTests
{
    private static readonly CancellationToken Ct = new CancellationTokenSource().Token;
    
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IEmployeeRepository> _employeeRepositoryMock;
    private readonly Mock<ILeaveRequestRepository> _leaveRequestRepositoryMock;
    private readonly Mock<ILogger<SubmitLeaveRequestCommandHandler>> _loggerMock;
    private readonly SubmitLeaveRequestCommandHandler _handler;
    

    public SubmitLeaveRequestCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _employeeRepositoryMock = new Mock<IEmployeeRepository>();
        _leaveRequestRepositoryMock = new Mock<ILeaveRequestRepository>();
        _loggerMock = new Mock<ILogger<SubmitLeaveRequestCommandHandler>>();
        
        _unitOfWorkMock.Setup(u => u.Employees).Returns(_employeeRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.LeaveRequests).Returns(_leaveRequestRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        _handler = new SubmitLeaveRequestCommandHandler(_unitOfWorkMock.Object, _loggerMock.Object);
        
        
    }
    
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);
    
    private static SubmitLeaveRequestCommand ValidCommand(Guid? employeeId=null) => new(
        EmployeeId: employeeId??Guid.NewGuid(),
        LeaveType: LeaveType.Annual,
        StartDate: Today.AddDays(7),
        EndDate: Today.AddDays(14));


    [Fact]
    public async Task Handle_WhenEmployeeNotFound_ShouldThrowNotFoundException()
    {
        //ARRANGE
        var employeeId = Guid.NewGuid();
        var command = ValidCommand(employeeId);
        
        _employeeRepositoryMock.Setup(r => r.GetStatusAsync(employeeId,Ct))
            .ReturnsAsync((EmployeeStatus?)null);
        
        //ACT
        var act = async () => await _handler.Handle(command, Ct);
        
        //ASSERT
        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*Employee*");
        
        _employeeRepositoryMock.Verify(r => r.GetStatusAsync(employeeId, Ct), Times.Once);
        _leaveRequestRepositoryMock.Verify(r => r.Add(It.IsAny<LeaveRequest>()), Times.Never);
        _unitOfWorkMock.Verify(r => r.SaveChangesAsync(Ct), Times.Never);
    }
    
    [Fact]
    public async Task Handle_WhenEmployeeFired_ShouldThrowBusinessRuleException()
    {
        //ARRANGE
        var employeeId = Guid.NewGuid();
        var command = ValidCommand(employeeId);
        
        _employeeRepositoryMock.Setup(r => r.GetStatusAsync(employeeId,Ct))
            .ReturnsAsync(EmployeeStatus.Fired);
        
        //ACT
        var act = async () => await _handler.Handle(command, Ct);
        
        //ASSERT
        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*fired*");
        
        _employeeRepositoryMock.Verify(r => r.GetStatusAsync(employeeId, Ct), Times.Once);
        _leaveRequestRepositoryMock.Verify(r => r.Add(It.IsAny<LeaveRequest>()), Times.Never);
        _unitOfWorkMock.Verify(r => r.SaveChangesAsync(Ct), Times.Never);
    }


    [Theory]
    [InlineData(EmployeeStatus.Active)]
    [InlineData(EmployeeStatus.OnVacation)]
    public async Task Handle_WhenEmployeeIsNotFired_ShouldCreatePendingRequest(EmployeeStatus status)
    {
        //ARRANGE
        var employeeId = Guid.NewGuid();
        var command = ValidCommand(employeeId);
        
        _employeeRepositoryMock.Setup(r => r.GetStatusAsync(employeeId, Ct))
            .ReturnsAsync(status);
        
        LeaveRequest? added = null;
        _leaveRequestRepositoryMock
            .Setup(r => r.Add(It.IsAny<LeaveRequest>()))
            .Callback<LeaveRequest>(r => added = r);
        //ACT
        var result =  await _handler.Handle(command, Ct);
        
        //ASSERT
        added.Should().NotBeNull();
        added!.Id.Should().NotBeEmpty();
        added.EmployeeId.Should().Be(command.EmployeeId);
        added.LeaveType.Should().Be(command.LeaveType);
        added.StartDate.Should().Be(command.StartDate);
        added.EndDate.Should().Be(command.EndDate);
        added.Status.Should().Be(LeaveStatus.Pending);
        added.DecidedAt.Should().BeNull();
        added.DecidedBy.Should().BeNull();
        
        result.Should().BeSameAs(added);
        
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(Ct), Times.Once);
    }
    
}