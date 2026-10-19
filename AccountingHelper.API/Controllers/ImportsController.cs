using AccountingHelper.Application.DTOs.Requests;
using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Application.Features.Imports.Commands.CreateImportJob;
using AccountingHelper.Application.Features.Imports.Queries.GetImportErrors;
using AccountingHelper.Application.Features.Imports.Queries.GetImportJob;
using AccountingHelper.Application.Mapping;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AccountingHelper.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/imports")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class ImportsController : ControllerBase
{
    private readonly ISender _sender;

    public ImportsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImportJobResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ImportJobResponse>> CreateImportJob(
        IFormFile file,
        CancellationToken ct = default)
    {
        // Old browsers sent the full client path, and on Linux Path.GetFileName does not split on '\'.
        var fileName = Path.GetFileName(file.FileName.Replace('\\', '/'));

        await using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, ct);

        var command = new CreateImportJobCommand(fileName, file.ContentType, buffer.ToArray());
        var job = await _sender.Send(command, ct);

        return AcceptedAtAction(nameof(GetImportJob), new { id = job.Id }, job);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ImportJobResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ImportJobResponse>> GetImportJob(
        Guid id,
        CancellationToken ct = default)
    {
        var job = await _sender.Send(new GetImportJobQuery(id), ct);
        return Ok(job);
    }

    // Paged, unlike the flat payslip list: a ten-thousand-row file can fail on most of its rows, and the
    // report being rows rather than a JSON column was chosen precisely so it can be read a page at a time.
    [HttpGet("{id:guid}/errors")]
    [ProducesResponseType(typeof(PagedResponse<ImportErrorResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<ImportErrorResponse>>> GetImportErrors(
        Guid id,
        [FromQuery] PaginationRequest request,
        CancellationToken ct = default)
    {
        var query = request.ToErrorsQuery(id);
        var (errors, total) = await _sender.Send(query, ct);

        return Ok(PagedResponse<ImportErrorResponse>.Create(errors, total, query.Limit, query.Offset));
    }
}