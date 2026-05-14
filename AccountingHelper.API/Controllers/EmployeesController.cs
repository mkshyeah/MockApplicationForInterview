using AccountingHelper.Application.DTOs.Requests;
using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Application.Interfaces;
using AccountingHelper.Application.Mapping;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;


namespace AccountingHelper.API.Controllers;

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
    
    [HttpGet]
    public async Task<IActionResult> GetEmployees(
        [FromQuery] EmployeeFilteredRequest request, 
        CancellationToken ct=default)
    {
        var employees = await _employeeService.GetEmployees(request, ct);
        var total = await _employeeService.CountEmployees(request, ct);
        
        var response = PagedResponse<EmployeeResponse>.Create(
            employees.Select(e => e.ToResponse()).ToList().AsReadOnly(),
            total,
            request.Limit,
            request.Offset);


        return Ok(response);
    }


    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetEmployee(Guid id, CancellationToken ct)
    {
        var result = await _employeeService.GetEmployee(id, ct);

        var response =  result.ToResponse();
        
        return Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> CreateEmployee(
        [FromBody] CreateEmployeeRequest request,
        CancellationToken ct)
    {
        var model = request.ToModel();
        var created = await _employeeService.CreateEmployee(model, ct);

        return CreatedAtAction(nameof(GetEmployee), new { id = created.Id }, created.ToResponse());
    }

    [HttpPatch("{id:guid}/fire")]
    public async Task<IActionResult> FireEmployee(Guid id, CancellationToken ct)
    {
        var result = await _employeeService.FireEmployee(id, ct);
        return Ok(result.ToResponse());
    }
}