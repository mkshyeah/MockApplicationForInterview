using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Application.Features.Imports.Commands.CreateImportJob;
using AccountingHelper.Application.Features.Imports.Queries.GetImportJob;
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
}