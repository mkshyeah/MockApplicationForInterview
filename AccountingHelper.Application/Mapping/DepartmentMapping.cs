using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Domain.Models;

namespace AccountingHelper.Application.Mapping;

public static class DepartmentMapping
{
    public static DepartmentResponse ToResponse(this Department department) => new()
    {
        Id = department.Id,
        Name = department.Name,
        Description = department.Description,
    };
}
