using System.Threading.Channels;
using AccountingHelper.Application.DTOs.Requests;
using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Application.Features.Salaries.ChangeSalary;
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

    public static ChangeSalaryCommand ToCommand(this ChangeSalaryRequest request, Guid employeeId) => new(
        EmployeeId: employeeId,
        Amount: request.Amount,
        SalaryType: request.SalaryType);
}