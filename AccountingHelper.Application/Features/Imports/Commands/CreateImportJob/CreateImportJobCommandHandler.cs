using System.Net.Http.Headers;
using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Application.Mapping;
using AccountingHelper.Contracts.Messaging;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AccountingHelper.Application.Features.Imports.Commands.CreateImportJob;

public class CreateImportJobCommandHandler : IRequestHandler<CreateImportJobCommand, ImportJobResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CreateImportJobCommandHandler> _logger;
    
    public CreateImportJobCommandHandler(IUnitOfWork unitOfWork, IPublishEndpoint publishEndpoint, ILogger<CreateImportJobCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }
    
    public async Task<ImportJobResponse> Handle(CreateImportJobCommand request, CancellationToken ct)
    {
        var jobId = Guid.NewGuid();

        var job = new ImportJob
        {
            Id = jobId,
            FileName = request.FileName,
            Status = ImportJobStatus.Pending,
        };

        var file = new ImportFile
        {
            Id = Guid.NewGuid(),
            ImportJobId = jobId,
            // The validator has already parsed it against the allow-list; header parameters are not worth storing.
            ContentType = MediaTypeHeaderValue.Parse(request.ContentType).MediaType!.ToLowerInvariant(),
            Content = request.Content
        };
        
        _unitOfWork.ImportJobs.Add(job);
        _unitOfWork.ImportJobs.AddFile(file);

        await _publishEndpoint.Publish(new ImportRequested(jobId), ct);
        await _unitOfWork.SaveChangesAsync(ct);
        
        _logger.LogInformation("Import job {ImportJobId} was accepted for file {FileName}.", job.Id, job.FileName);

        return job.ToResponse();
    }
}