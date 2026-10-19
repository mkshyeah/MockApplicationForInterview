using MediatR;

namespace AccountingHelper.Application.Features.Imports.Commands.RunImport;

public record RunImportCommand(Guid ImportJobId) : IRequest;
