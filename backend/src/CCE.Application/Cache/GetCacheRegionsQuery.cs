using CCE.Application.Common;
using CCE.Application.Common.Caching;
using MediatR;

namespace CCE.Application.Cache;

/// <summary>Lists the cache regions ("tables") and how many entries each currently holds.</summary>
public sealed record GetCacheRegionsQuery : IRequest<Response<IReadOnlyList<CacheRegionStatus>>>;
