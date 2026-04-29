using AccountingHelper.Contexts;
using AccountingHelper.Extensions;
using AccountingHelper.Models;
using AccountingHelper.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccountingHelper.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/employees")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }
    
    [HttpGet()]
    public async Task<IActionResult> GetEmployees(
        [FromQuery] PaginationParams pagination, 
        CancellationToken ct)
    {
        var result = await _employeeService
            .GetEmployees(pagination.Page, pagination.PageSize, ct);
        
        var response = result.Select(e => e.ToResponse()).ToList();

        return Ok(response);
    }


    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetEmployee(Guid id, CancellationToken ct)
    {
        var result = await _employeeService.GetEmployee(id, ct);
        
        var response = result?.ToResponse();

        return Ok(response);
    }

    [HttpPost()]
    public async Task<IActionResult> CreateEmployee(
        [FromBody] CreateEmployeeRequest request,
        CancellationToken ct)
    {
        var employee = await _employeeService.CreateEmployee(request.ToModel(), ct);
        
        var response = employee.ToResponse();
        
        return CreatedAtAction(nameof(GetEmployee), new {id = response.Id}, response);
    }

    [HttpPut("{id:guid}/fire")]
    public async Task<IActionResult> FireEmployee(Guid id, CancellationToken ct)
    {
        var result = await _employeeService.FireEmployee(id, ct);
        var response = result.ToResponse();
        return Ok(response);
    }
}