using AccountingHelper.Domain.Models;

namespace AccountingHelper.Domain.Interfaces;

public interface IPayslipRepository
{
    void AddRange(IEnumerable<Payslip> payslips);
}