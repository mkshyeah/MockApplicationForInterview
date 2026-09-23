using AccountingHelper.Application.DTOs.Responses;
using MediatR;

namespace AccountingHelper.Application.Features.Imports.Commands.CreateImportJob;

public record CreateImportJobCommand(
    string FileName,
    string ContentType,
    byte[] Content) : IRequest<ImportJobResponse>;