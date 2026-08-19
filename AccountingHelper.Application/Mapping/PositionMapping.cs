using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Domain.Models;

namespace AccountingHelper.Application.Mapping;

public static class PositionMapping
{
    public static PositionResponse ToResponse(this Position position) => new()
    {
        Id = position.Id,
        Title = position.Title,
        Description = position.Description,
        Grade = position.Grade,
    };
}
