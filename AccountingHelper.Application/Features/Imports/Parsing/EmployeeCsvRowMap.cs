using CsvHelper.Configuration;

namespace AccountingHelper.Application.Features.Imports.Parsing;

// Header names are the file's contract with the operator, so they are spelled out rather than
// inferred from property names a refactoring could change.
public class EmployeeCsvRowMap : ClassMap<EmployeeCsvRow>
{
    public EmployeeCsvRowMap()
    {
        Map(r => r.FirstName).Name("FirstName"); 
        Map(r => r.LastName).Name("LastName"); 
        Map(r => r.Email).Name("Email"); 
        Map(r => r.Department).Name("Department"); 
        Map(r => r.Position).Name("Position"); 
        Map(r => r.SalaryAmount).Name("SalaryAmount"); 
        Map(r => r.SalaryType).Name("SalaryType"); 
        Map(r => r.HireDate).Name("HireDate");
    }
}