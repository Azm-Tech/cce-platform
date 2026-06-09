using CCE.Application.Common;
using CCE.Application.Common.Caching;
using FluentValidation;
using MediatR;

namespace CCE.Application.Cache;

/// <summary>Purges every cached entry in one region (used by both reload and delete-region endpoints).</summary>
public sealed record EvictCacheRegionCommand(string Region) : IRequest<Response<VoidData>>;

public sealed class EvictCacheRegionCommandValidator : AbstractValidator<EvictCacheRegionCommand>
{
    public EvictCacheRegionCommandValidator()
    {
        RuleFor(x => x.Region)
            .NotEmpty().WithErrorCode("REQUIRED_FIELD")
            .Must(CacheRegions.IsKnownRegion).WithErrorCode("INVALID_ENUM");
    }
}
