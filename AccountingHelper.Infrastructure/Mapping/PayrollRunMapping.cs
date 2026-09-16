using AccountingHelper.Domain.Models;
using AccountingHelper.Infrastructure.Data.Entities;

namespace AccountingHelper.Infrastructure.Mapping;

public static class PayrollRunMapping
{
    public static PayrollRun ToModel(this PayrollRunEntity entity) => new ()
    {
        Id = entity.Id,
        Year = entity.Year,
        Month = entity.Month,
        Status = entity.Status,
        ProcessedCount = entity.ProcessedCount,
        SkippedCount = entity.SkippedCount,
        StartedAt = entity.StartedAt,
        CompletedAt = entity.CompletedAt,
        Payslips = entity.Payslips.Select(e => e.ToModel()).ToList()
    };



    public static PayrollRunEntity ToEntity(this PayrollRun model) => new()
    {
        Id = model.Id,
        Year = model.Year,
        Month = model.Month,
        Status = model.Status,
        ProcessedCount = model.ProcessedCount,
        SkippedCount = model.SkippedCount,
        StartedAt = model.StartedAt,
        CompletedAt = model.CompletedAt
    };
}