using AccountingHelper.Application.Caching;
using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Application.Mapping;
using AccountingHelper.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace AccountingHelper.Application.Features.Departments.Queries.GetDepartments;

public class GetDepartmentsQueryHandler : IRequestHandler<GetDepartmentsQuery, IReadOnlyList<DepartmentResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly HybridCache _cache;
    private readonly HybridCacheEntryOptions _cacheOptions;

    public GetDepartmentsQueryHandler(
        IUnitOfWork unitOfWork,
        HybridCache cache,
        IOptions<CacheSettings> cacheSettings)
    {
        _unitOfWork = unitOfWork;
        _cache = cache;
        _cacheOptions = new HybridCacheEntryOptions
        {
            Expiration = cacheSettings.Value.Lookups,
            LocalCacheExpiration = cacheSettings.Value.Lookups
        };
    }

    public async Task<IReadOnlyList<DepartmentResponse>> Handle(
        GetDepartmentsQuery request,
        CancellationToken ct)
    {
        return await _cache.GetOrCreateAsync(
            CacheKeys.LookupsDepartments,
            this,
            static (handler, token) => handler.BuildAsync(token),
            _cacheOptions,
            cancellationToken: ct);
    }

    private async ValueTask<IReadOnlyList<DepartmentResponse>> BuildAsync(CancellationToken ct)
    {
        var items = await _unitOfWork.Departments.GetAllAsync(ct);

        return items.Select(x => x.ToResponse()).ToList();
    }
}