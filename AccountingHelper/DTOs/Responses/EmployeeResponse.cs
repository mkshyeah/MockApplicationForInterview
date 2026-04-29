using AccountingHelper.Models.Enums;

namespace AccountingHelper.Models;

public class EmployeeResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } 
    public string Position { get; set; }
    public string Status { get; set; } 
}