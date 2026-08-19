using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Services;
using MediatR;

namespace AccountingHelper.Application.Features.Reports.Queries.GetDashboard;

public class GetDashboardQueryHandler : IRequestHandler<GetDashboardQuery, DashboardResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetDashboardQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<DashboardResponse> Handle(GetDashboardQuery request, CancellationToken ct)
    {
        var snapshots = await _unitOfWork.Salaries.GetActiveSnapshotsAsync(ct);
        var headcounts = await _unitOfWork.Employees.GetHeadcountByDepartmentAsync(ct);

        // Деньги считаются в памяти, а не в SQL, намеренно. Ни приведение к месячной сумме,
        // ни налог не выражаются агрегатом БД: и то и другое — правило домена, живущее в
        // SalaryCalculator (коэффициенты типов оплаты, прогрессивная шкала). SUM в базе
        // означал бы либо перенос этих правил в SQL и их раздвоение, либо суммирование
        // несопоставимых величин — часовой ставки с месячным окладом. В базе остаётся
        // считаться только то, что от домена не зависит: headcount, обычный COUNT с GROUP BY.
        // Объём данных здесь — активные зарплаты, по одной на работающего сотрудника; когда
        // их станет столько, что пересылка начнёт мешать, лечится это предагрегацией или
        // materialized view, а не переписыванием домена на SQL.
        var monthly = snapshots
            .Select(s => new
            {
                s.Grade,
                Amount = SalaryCalculator.Convert(s.Amount, s.Type, SalaryType.Monthly),
                Tax = SalaryCalculator.CalculateTaxes(s.Amount, s.Type)
            })
            .ToList();

        return new DashboardResponse
        {
            TotalMonthlyPayroll = Round(monthly.Sum(x => x.Amount)),
            TotalMonthlyTax = Round(monthly.Sum(x => x.Tax)),

            HeadcountByDepartment = headcounts
                // Ordinal, а не культурное сравнение: порядок ответа не должен зависеть
                // от локали машины, где крутится процесс.
                .OrderBy(h => h.DepartmentName, StringComparer.Ordinal)
                .Select(h => new DepartmentHeadcountResponse
                {
                    DepartmentId = h.DepartmentId,
                    DepartmentName = h.DepartmentName,
                    Headcount = h.Headcount
                })
                .ToList(),

            AverageSalaryByGrade = monthly
                .GroupBy(x => x.Grade)
                .OrderBy(g => g.Key)
                .Select(g => new GradeAverageSalaryResponse
                {
                    Grade = g.Key,
                    AverageMonthlySalary = Round(g.Average(x => x.Amount))
                })
                .ToList()
        };
    }
    
    private static decimal Round(decimal val) => Math.Round(val, 2, MidpointRounding.AwayFromZero);
}