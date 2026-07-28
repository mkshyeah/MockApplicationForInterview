using MediatR;

namespace AccountingHelper.Application.Features.Reports.Queries.GetCalculateTaxes;

public record GetCalculateTaxesQuery(Guid EmployeeId) : IRequest<decimal>;