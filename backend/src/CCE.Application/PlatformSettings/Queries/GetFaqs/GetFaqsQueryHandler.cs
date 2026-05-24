using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Messages;
using CCE.Application.PlatformSettings.Dtos;
using MediatR;

namespace CCE.Application.PlatformSettings.Queries.GetFaqs;

public sealed class GetFaqsQueryHandler
    : IRequestHandler<GetFaqsQuery, Response<IReadOnlyList<FaqDto>>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public GetFaqsQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<IReadOnlyList<FaqDto>>> Handle(
        GetFaqsQuery request, CancellationToken cancellationToken)
    {
        var faqs = await _db.Faqs
            .OrderBy(f => f.Order)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        var dtos = faqs.Select(f => new FaqDto(
            f.Id,
            new LocalizedTextDto(f.Question.Ar, f.Question.En),
            new LocalizedTextDto(f.Answer.Ar, f.Answer.En),
            f.Order)).ToList();

        return _msg.Ok<IReadOnlyList<FaqDto>>(dtos, "ITEMS_LISTED");
    }
}
