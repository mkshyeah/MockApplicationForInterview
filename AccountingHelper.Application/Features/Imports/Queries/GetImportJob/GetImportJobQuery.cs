using AccountingHelper.Application.DTOs.Responses;
using MediatR;

namespace AccountingHelper.Application.Features.Imports.Queries.GetImportJob;

public record GetImportJobQuery(Guid ImportJobId) : IRequest<ImportJobResponse>;
