using AccountingHelper.Application.DTOs.Responses;
using MediatR;

namespace AccountingHelper.Application.Features.Employees.Queries.GetEmployee;

public record GetEmployeeQuery(Guid EmployeeId) : IRequest<EmployeeResponse>;