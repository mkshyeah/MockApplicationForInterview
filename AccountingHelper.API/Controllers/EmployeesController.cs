using AccountingHelper.Application.DTOs.Requests;
using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Application.Interfaces;
using AccountingHelper.Application.Mapping;
using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using AccountingHelper.Application.Exceptions;
using ValidationException = AccountingHelper.Application.Exceptions.ValidationException;


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
    [ProducesResponseType(typeof(PagedResponse<EmployeeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetEmployees(
        [FromQuery] EmployeeFilteredRequest request,
        [FromServices] IValidator<EmployeeFilteredRequest> validator,
        CancellationToken ct=default)
    {
        var validationResult = await validator.ValidateAsync(request);
        
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }
        
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
    [ProducesResponseType(typeof(EmployeeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEmployee(Guid id, CancellationToken ct=default)
    {
        var result = await _employeeService.GetEmployee(id, ct);

        var response =  result.ToResponse();
        
        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(EmployeeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateEmployee(
        [FromBody] CreateEmployeeRequest request,
        [FromServices] IValidator<CreateEmployeeRequest> validator,
        CancellationToken ct=default)
    {
        var result = await validator.ValidateAsync(request);
        
        if (!result.IsValid)
            throw new ValidationException(result.Errors);
        
        var model = request.ToModel();
        var created = await _employeeService.CreateEmployee(model, ct);

        return CreatedAtAction(nameof(GetEmployee), new { id = created.Id }, created.ToResponse());
    }

    [HttpPatch("{id:guid}/fire")]
    [ProducesResponseType(typeof(EmployeeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> FireEmployee(Guid id, CancellationToken ct=default)
    {
        var result = await _employeeService.FireEmployee(id, ct);
        return Ok(result.ToResponse());
    }

    [HttpPatch("{id:guid}/on-vacation")]
    [ProducesResponseType(typeof(EmployeeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SendOnVacation(Guid id, CancellationToken ct = default)
    {
        var result = await _employeeService.SendOnVacation(id, ct);
        return Ok(result.ToResponse());
    }
    
    [HttpPatch("{id:guid}/off-vacation")]
    [ProducesResponseType(typeof(EmployeeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SendOffVacation(Guid id, CancellationToken ct = default)
    {
        var result = await _employeeService.SendOffVacation(id, ct);
        return Ok(result.ToResponse());
    }
}