using AccountingHelper.Application.Features.Reports.Queries.GetEmployeeCount;
using AccountingHelper.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace AccountingHelper.UnitTests.Application.Handlers.Reports;

public class GetEmployeeCountQueryHandlerTests
{
    // A distinct token (never CancellationToken.None) so every Setup/Verify below
    // proves the handler forwards the caller's token down each awaited call.
    private static readonly CancellationToken Ct = new CancellationTokenSource().Token;

    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IEmployeeRepository> _employeeRepositoryMock;
    private readonly GetEmployeeCountQueryHandler _handler;

    public GetEmployeeCountQueryHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _employeeRepositoryMock = new Mock<IEmployeeRepository>();

        _unitOfWorkMock.Setup(u => u.Employees).Returns(_employeeRepositoryMock.Object);

        _handler = new GetEmployeeCountQueryHandler(_unitOfWorkMock.Object);
    }

    // The handler holds no logic — it delegates straight to the repository, so what
    // the count actually means is proven in ReportingTests. The one thing worth
    // pinning here is that the caller's token reaches the repository.
    [Fact]
    public async Task Handle_ShouldReturnRepositoryCount_AndForwardToken()
    {
        // ARRANGE
        var query = new GetEmployeeCountQuery();

        _employeeRepositoryMock
            .Setup(r => r.CountAsync(Ct))
            .ReturnsAsync(42);

        // ACT
        var result = await _handler.Handle(query, Ct);

        // ASSERT
        result.Should().Be(42);

        _employeeRepositoryMock.Verify(r => r.CountAsync(Ct), Times.Once);
    }
}
