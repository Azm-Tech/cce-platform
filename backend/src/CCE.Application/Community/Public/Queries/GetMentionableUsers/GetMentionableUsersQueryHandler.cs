using System.Collections.Generic;
using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Community.Public.Dtos;
using CCE.Application.Messages;
using MediatR;
using CCE.Application.Community;

namespace CCE.Application.Community.Public.Queries.GetMentionableUsers;

public sealed class GetMentionableUsersQueryHandler
    : IRequestHandler<GetMentionableUsersQuery, Response<IReadOnlyList<MentionableUserDto>>>
{
    private readonly IReplyRepository _repo;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly MessageFactory _msg;

    public GetMentionableUsersQueryHandler(IReplyRepository repo, ICurrentUserAccessor currentUser, MessageFactory msg)
    {
        _repo = repo;
        _currentUser = currentUser;
        _msg = msg;
    }

    public async Task<Response<IReadOnlyList<MentionableUserDto>>> Handle(
        GetMentionableUsersQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();
        if (userId is null || userId == System.Guid.Empty)
            return _msg.Unauthorized<IReadOnlyList<MentionableUserDto>>(MessageKeys.Identity.NOT_AUTHENTICATED);

        if (string.IsNullOrWhiteSpace(request.Q) || request.Q.Length < 2)
            return _msg.Ok<IReadOnlyList<MentionableUserDto>>(
                System.Array.Empty<MentionableUserDto>(), MessageKeys.General.ITEMS_LISTED);

        var results = await _repo.SearchMentionableAsync(
            request.CommunityId, userId.Value, request.Q.Trim(),
            System.Math.Clamp(request.Limit, 1, 20), cancellationToken)
            .ConfigureAwait(false);

        return _msg.Ok<IReadOnlyList<MentionableUserDto>>(results, MessageKeys.General.ITEMS_LISTED);
    }
}
