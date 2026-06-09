using CCE.Application.Common;
using FluentValidation;
using MediatR;

namespace CCE.Application.Cache;

/// <summary>Deletes a single output-cache entry by its full Redis key (e.g. <c>out:/api/resources?page=1|lang=en</c>).</summary>
public sealed record EvictCacheKeyCommand(string Key) : IRequest<Response<VoidData>>;

public sealed class EvictCacheKeyCommandValidator : AbstractValidator<EvictCacheKeyCommand>
{
    public EvictCacheKeyCommandValidator()
        => RuleFor(x => x.Key).NotEmpty().WithErrorCode("REQUIRED_FIELD");
}
