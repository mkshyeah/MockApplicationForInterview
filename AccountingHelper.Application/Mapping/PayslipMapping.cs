using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Domain.Models;

namespace AccountingHelper.Application.Mapping;

public static class PayslipMapping
{
    public static PayslipResponse ToResponse(this Payslip model) => new PayslipResponse
    {
        Id = model.Id,
        EmployeeId = model.EmployeeId,
        Gross = model.Gross,
        Tax = model.Tax,
        Deductions = model.Deductions,
        Net = model.Net
    };
}