using AccountingHelper.Domain.Models;
using MediatR;

namespace AccountingHelper.Application.Features.Departments.Queries.GetDepartments;

public record GetDepartmentsQuery : IRequest<IReadOnlyList<Department>>;
