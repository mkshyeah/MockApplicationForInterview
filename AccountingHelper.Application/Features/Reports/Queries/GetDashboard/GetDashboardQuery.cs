using AccountingHelper.Application.DTOs.Responses;
using MediatR;

namespace AccountingHelper.Application.Features.Reports.Queries.GetDashboard;

public record GetDashboardQuery : IRequest<DashboardResponse>;