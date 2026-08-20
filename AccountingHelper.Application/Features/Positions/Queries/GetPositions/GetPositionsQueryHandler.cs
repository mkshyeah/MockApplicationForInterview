using AccountingHelper.Application.Caching;
using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Application.Features.Departments.Queries.GetDepartments;
using AccountingHelper.Application.Mapping;
using AccountingHelper.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace AccountingHelper.Application.Features.Positions.Queries.GetPositions;

public class GetPositionsQueryHandler : IRequestHandler<GetPositionsQuery, IReadOnlyList<PositionResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly HybridCache _cache;
    private readonly HybridCacheEntryOptions _cacheOptions;

    public GetPositionsQueryHandler(
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
    
    public async Task<IReadOnlyList<PositionResponse>> Handle(GetPositionsQuery request, CancellationToken ct)
    {
        return await _cache.GetOrCreateAsync(
            CacheKeys.LookupsPositions,
            this,
            static (handler, token) => handler.BuildAsync(token),
            _cacheOptions,
            cancellationToken: ct);
    }

    private async ValueTask<IReadOnlyList<PositionResponse>> BuildAsync(CancellationToken ct)
    {
        var items = await _unitOfWork.Positions.GetAllAsync(ct);

        return items.Select(x => x.ToResponse()).ToList();
    }
}
