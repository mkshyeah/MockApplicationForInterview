using AccountingHelper.Domain.Models;
using AccountingHelper.Infrastructure.Data.Entities;

namespace AccountingHelper.Infrastructure.Mapping;

public static class PayslipMapping
{
    public static Payslip ToModel(this PayslipEntity entity) => new()
    {
        Id = entity.Id,
        PayrollRunId = entity.PayrollRunId,
        EmployeeId = entity.EmployeeId,
        Gross = entity.Gross,
        Tax = entity.Tax,
        Deductions = entity.Deductions,
        Net = entity.Net
    };

    public static PayslipEntity ToEntity(this Payslip model) => new()
    {
        Id = model.Id,
        PayrollRunId = model.PayrollRunId,
        EmployeeId = model.EmployeeId,
        Gross = model.Gross,
        Tax = model.Tax,
        Deductions = model.Deductions,
        Net = model.Net
    };
}