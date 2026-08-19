using AccountingHelper.Application.DTOs.Responses;
using MediatR;

namespace AccountingHelper.Application.Features.Departments.Queries.GetDepartments;

public record GetDepartmentsQuery : IRequest<IReadOnlyList<DepartmentResponse>>;
