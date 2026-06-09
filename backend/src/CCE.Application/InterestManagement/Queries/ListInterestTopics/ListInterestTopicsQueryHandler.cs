using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.InterestManagement.Dtos;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.InterestManagement.Queries.ListInterestTopics;

public sealed class ListInterestTopicsQueryHandler
    : IRequestHandler<ListInterestTopicsQuery, Response<IReadOnlyList<InterestTopicDto>>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public ListInterestTopicsQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<IReadOnlyList<InterestTopicDto>>> Handle(
        ListInterestTopicsQuery request, CancellationToken cancellationToken)
    {
        var topics = await _db.InterestTopics
            .OrderBy(t => t.NameEn)
            .Select(t => new InterestTopicDto(t.Id, t.NameAr, t.NameEn, t.IsActive))
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        return _msg.Ok<IReadOnlyList<InterestTopicDto>>(topics, "SUCCESS_OPERATION");
    }
}
