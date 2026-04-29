using AccountingHelper.Models.Enums;

namespace AccountingHelper.Models;

public class CreateEmployeeRequest
{
    public string FirstName { get; set; }
    
    public string LastName { get; set; }

    public string Email { get; set; }

    public string Position { get; set; }

    public decimal Salary { get; set; }

    public string Department { get; set; }
    
    public DateTime HireDate { get; set; }
}