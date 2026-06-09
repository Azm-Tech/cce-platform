using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Cache;

/// <summary>Purges every known cache region.</summary>
public sealed record FlushCacheCommand : IRequest<Response<VoidData>>;
