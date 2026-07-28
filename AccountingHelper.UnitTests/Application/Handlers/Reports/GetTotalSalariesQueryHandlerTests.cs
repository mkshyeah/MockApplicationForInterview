using AccountingHelper.Application.Features.Reports.Queries.GetTotalSalaries;
using AccountingHelper.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace AccountingHelper.UnitTests.Application.Handlers.Reports;

public class GetTotalSalariesQueryHandlerTests
{
    // A distinct token (never CancellationToken.None) so every Setup/Verify below
    // proves the handler forwards the caller's token down each awaited call.
    private static readonly CancellationToken Ct = new CancellationTokenSource().Token;

    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ISalaryRepository> _salaryRepositoryMock;
    private readonly GetTotalSalariesQueryHandler _handler;

    public GetTotalSalariesQueryHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _salaryRepositoryMock = new Mock<ISalaryRepository>();

        _unitOfWorkMock.Setup(u => u.Salaries).Returns(_salaryRepositoryMock.Object);

        _handler = new GetTotalSalariesQueryHandler(_unitOfWorkMock.Object);
    }

    // The handler holds no logic — it delegates straight to the repository. That only
    // *active* salaries are summed is a repository/SQL concern, proven in ReportingTests.
    [Fact]
    public async Task Handle_ShouldReturnRepositoryTotal_AndForwardToken()
    {
        // ARRANGE
        var query = new GetTotalSalariesQuery();

        _salaryRepositoryMock
            .Setup(r => r.GetTotalCurrentSalaryAsync(Ct))
            .ReturnsAsync(7_500m);

        // ACT
        var result = await _handler.Handle(query, Ct);

        // ASSERT
        result.Should().Be(7_500m);

        _salaryRepositoryMock.Verify(r => r.GetTotalCurrentSalaryAsync(Ct), Times.Once);
    }
}
