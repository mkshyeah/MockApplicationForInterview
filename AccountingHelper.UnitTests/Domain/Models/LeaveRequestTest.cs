using System.Globalization;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Models;
using FluentAssertions;

namespace AccountingHelper.UnitTests.Domain.Models;

public class LeaveRequestTest
{
    [Theory]
    // Обычный интервал внутри одного месяца: 5, 6, 7, 8, 9 апреля
    [InlineData("2026-04-05", "2026-04-09", 5)]

    // Один день: обе границы включительны, значит 1, а не 0
    [InlineData("2026-04-05", "2026-04-05", 1)]

    // Переход через границу месяца: 31 января + 1 февраля
    [InlineData("2026-01-31", "2026-02-01", 2)]

    // Переход через границу года: 31 декабря + 1 января
    [InlineData("2025-12-31", "2026-01-01", 2)]

    // Високосный февраль: 28 и 29 февраля + 1 марта
    [InlineData("2024-02-28", "2024-03-01", 3)]

    // Тот же интервал в невисокосный год: 29 февраля не существует, поэтому на день меньше
    [InlineData("2026-02-28", "2026-03-01", 2)]
    public void DurationInDays_ShouldCountBothEndsInclusive_AcrossCalendarBoundaries(
        string startDate,
        string endDate,
        int expectedDays)
    {
        // Arrange
        var leaveRequest = new LeaveRequest
        {
            EmployeeId = Guid.NewGuid(),
            LeaveType = LeaveType.Annual,
            StartDate = ParseDate(startDate),
            EndDate = ParseDate(endDate),
            Status = LeaveStatus.Pending,
            RequestedAt = DateTime.UtcNow
        };

        // Act
        var actualDays = leaveRequest.DurationInDays;

        // Assert
        actualDays.Should().Be(expectedDays);
    }

    // DateOnly нельзя передать через InlineData (в атрибутах допустимы только константы),
    // поэтому даты приходят строками и разбираются здесь.
    private static DateOnly ParseDate(string value) =>
        DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
}
