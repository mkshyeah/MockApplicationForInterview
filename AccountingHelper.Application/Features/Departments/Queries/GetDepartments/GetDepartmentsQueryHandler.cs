using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Application.Mapping;
using AccountingHelper.Domain.Interfaces;
using MediatR;

namespace AccountingHelper.Application.Features.Departments.Queries.GetDepartments;

public class GetDepartmentsQueryHandler : IRequestHandler<GetDepartmentsQuery, IReadOnlyList<DepartmentResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetDepartmentsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<DepartmentResponse>> Handle(GetDepartmentsQuery request, CancellationToken ct)
    {
        var items = await _unitOfWork.Departments.GetAllAsync(ct);

        return items.Select(x => x.ToResponse()).ToList();
    }
}
