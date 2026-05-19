using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Domain.Models;

namespace AccountingHelper.Application.Mapping;

public static class SalaryMapping
{
    public static SalaryResponse ToResponse(this Salary model) => new()
    {
        Id = model.Id,
        Amount = model.Amount,
        Type = model.Type,
        EffectiveDate = model.EffectiveDate,
        EndDate = model.EndDate,
    };
}