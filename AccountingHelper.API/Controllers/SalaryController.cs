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
[Route("v{version:apiVersion}/employees/{employeeId:guid}/salaries")]
public class SalaryController : ControllerBase
{
    private readonly ISalaryService _salaryService;

    public SalaryController(ISalaryService salaryService)
    {
        _salaryService = salaryService;
    }

    [HttpPut]
    [ProducesResponseType(typeof(SalaryResponse), StatusCodes.Status200OK)] 
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeSalary(
        Guid employeeId,
        [FromBody] ChangeSalaryRequest request,
        [FromServices] IValidator<ChangeSalaryRequest> validator,
        CancellationToken ct = default)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);
        
        var result = await _salaryService.ChangeSalary(employeeId, request.SalaryType, request.Amount, ct);
        
        return Ok(result.ToResponse());
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SalaryResponse>), StatusCodes.Status200OK)] 
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSalaryHistory(Guid employeeId, CancellationToken ct = default)
    {
        var result = await _salaryService.GetSalaryHistory(employeeId, ct);
        
        var response = result.Select(s => s.ToResponse()).ToList().AsReadOnly();
        return Ok(response);
    }
    
}