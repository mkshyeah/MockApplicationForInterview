using AccountingHelper.Domain.Models;
using AccountingHelper.Infrastructure.Data.Entities;

namespace AccountingHelper.Infrastructure.Mapping;

public static class SalaryMapping
{
    public static Salary ToModel(this SalaryEntity entity) => new()
    {
        Id = entity.Id,
        Amount = entity.Amount,
        EffectiveDate = entity.EffectiveDate,
        EndDate = entity.EndDate,
        Type = entity.Type,
        EmployeeId = entity.EmployeeId
    };

    public static SalaryEntity ToEntity(this Salary model) => new()
    {
        Id = model.Id,
        Amount = model.Amount,
        EffectiveDate = model.EffectiveDate.ToUniversalTime(),
        EndDate = model.EndDate?.ToUniversalTime(),
        Type = model.Type,
        EmployeeId = model.EmployeeId
    };
}