using AccountingHelper.DTOs.Requests;
using AccountingHelper.Extensions;
using AccountingHelper.Services.Interfaces;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;


namespace AccountingHelper.Controllers;

[ApiVersion("1.0")]
[Route("v{version:apiVersion}/employees")]
public class EmployeesController : ApiController
{
    private readonly IEmployeeService _employeeService;

    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetEmployees(
        [FromQuery] PaginationParams pagination, 
        CancellationToken ct)
    {
        var result = await _employeeService
            .GetEmployees(pagination.Page, pagination.PageSize, ct);


        return result.Match(
            employees => Ok(employees.Select(e => e.ToResponse()).ToList()),
            errors => Problem(errors)
            );
    }


    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetEmployee(Guid id, CancellationToken ct)
    {
        var result = await _employeeService.GetEmployee(id, ct);

        return result.Match(
            employee => Ok(employee.ToResponse()),
            errors => Problem(errors)
        );
    }

    [HttpPost]
    public async Task<IActionResult> CreateEmployee(
        [FromBody] CreateEmployeeRequest request,
        CancellationToken ct)
    {
        var result = await _employeeService.CreateEmployee(request.ToModel(), ct);

        return result.Match(
            employee =>
            {
                var response = employee.ToResponse();
                return CreatedAtAction(
                    nameof(GetEmployee),
                    new { id = response.Id },
                    response
                );
            },
            errors => Problem(errors)
            );
    }

    [HttpPatch("{id:guid}/fire")]
    public async Task<IActionResult> FireEmployee(Guid id, CancellationToken ct)
    {
        var result = await _employeeService.FireEmployee(id, ct);

        return result.Match(
            employee => Ok(employee.ToResponse()), 
            errors => Problem(errors)
            );
    }
}