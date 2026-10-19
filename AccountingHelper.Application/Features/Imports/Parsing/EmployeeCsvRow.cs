namespace AccountingHelper.Application.Features.Imports.Parsing;

// One record of the file as text. Nothing is typed on purpose: a bad value has to become a row error
// with a message, not a CsvHelper TypeConverterException halfway through a batch.
public class EmployeeCsvRow
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Department { get; set; }
    public string? Position { get; set; }
    public string? SalaryAmount { get; set; }
    public string? SalaryType { get; set; }
    public string? HireDate { get; set; }
}