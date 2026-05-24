using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Application.PlatformSettings.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.PlatformSettings.Queries.GetFaqById;

public sealed class GetFaqByIdQueryHandler
    : IRequestHandler<GetFaqByIdQuery, Response<FaqDto?>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public GetFaqByIdQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<FaqDto?>> Handle(
        GetFaqByIdQuery request, CancellationToken cancellationToken)
    {
        var faq = await _db.Faqs
            .FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken)
            .ConfigureAwait(false);

        if (faq is null)
            return _msg.FaqNotFound<FaqDto?>();

        var dto = new FaqDto(
            faq.Id,
            new LocalizedTextDto(faq.Question.Ar, faq.Question.En),
            new LocalizedTextDto(faq.Answer.Ar, faq.Answer.En),
            faq.Order);

        return _msg.Ok<FaqDto?>(dto, "ITEMS_LISTED");
    }
}
