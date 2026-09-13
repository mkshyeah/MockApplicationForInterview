using AccountingHelper.Application.DTOs.Requests;
using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Application.Features.Payroll.Commands.CreatePayrollRun;
using AccountingHelper.Domain.Models;

namespace AccountingHelper.Application.Mapping;

public static class PayrollRunMapping
{
    public static PayrollRunResponse ToResponse(this PayrollRun model) => new()
    {
        Id = model.Id,
        Year = model.Year,
        Month = model.Month,
        Status = model.Status
    };

    public static CreatePayrollRunCommand ToCommand(this CreatePayrollRunRequest request) => new(
        Year: request.Year,
        Month: request.Month);
}