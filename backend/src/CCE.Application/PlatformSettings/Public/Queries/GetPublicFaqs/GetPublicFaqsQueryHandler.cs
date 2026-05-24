using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Messages;
using CCE.Application.PlatformSettings.Dtos;
using CCE.Application.PlatformSettings.Public.Dtos;
using MediatR;

namespace CCE.Application.PlatformSettings.Public.Queries.GetPublicFaqs;

public sealed class GetPublicFaqsQueryHandler
    : IRequestHandler<GetPublicFaqsQuery, Response<IReadOnlyList<PublicFaqDto>>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public GetPublicFaqsQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<IReadOnlyList<PublicFaqDto>>> Handle(
        GetPublicFaqsQuery request, CancellationToken cancellationToken)
    {
        var faqs = await _db.Faqs
            .OrderBy(f => f.Order)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        var dtos = faqs.Select(f => new PublicFaqDto(
            new LocalizedTextDto(f.Question.Ar, f.Question.En),
            new LocalizedTextDto(f.Answer.Ar, f.Answer.En),
            f.Order)).ToList();

        return _msg.Ok<IReadOnlyList<PublicFaqDto>>(dtos, "ITEMS_LISTED");
    }
}
