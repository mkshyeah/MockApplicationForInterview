using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Domain.Models;

namespace AccountingHelper.Application.Mapping;

public static class DepartmentMapping
{
    public static DepartmentResponse ToResponse(this Department model) => new()
    {
        Id = model.Id,
        Name = model.Name,
        Description = model.Description
    };
    
}