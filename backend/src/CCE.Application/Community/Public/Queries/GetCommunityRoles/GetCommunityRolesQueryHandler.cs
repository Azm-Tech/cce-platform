using System.Collections.Generic;
using CCE.Application.Common;
using CCE.Application.Community.Public.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Public.Queries.GetCommunityRoles;

/// <summary>
/// Returns the fixed community role definitions as static config (no DB). Mirrors the
/// <see cref="CommunityRole"/> enum.
/// </summary>
public sealed class GetCommunityRolesQueryHandler
    : IRequestHandler<GetCommunityRolesQuery, Response<IReadOnlyList<CommunityRoleDto>>>
{
    private static readonly string[] MemberCapabilities =
        { "CreatePost", "Reply", "Vote", "VotePoll", "Follow" };

    private static readonly string[] ModeratorCapabilities =
        { "CreatePost", "Reply", "Vote", "VotePoll", "Follow", "ModerateContent", "ManageMembers", "ManageJoinRequests" };

    private readonly MessageFactory _msg;

    public GetCommunityRolesQueryHandler(MessageFactory msg)
    {
        _msg = msg;
    }

    public Task<Response<IReadOnlyList<CommunityRoleDto>>> Handle(
        GetCommunityRolesQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<CommunityRoleDto> roles = new List<CommunityRoleDto>
        {
            new(
                nameof(CommunityRole.Member),
                "Member",
                "عضو",
                "A community member who can create posts, reply, vote, and participate in polls.",
                "عضو في المجتمع يمكنه إنشاء المنشورات والرد والتصويت والمشاركة في الاستطلاعات.",
                MemberCapabilities),
            new(
                nameof(CommunityRole.Moderator),
                "Moderator",
                "مشرف",
                "A community moderator who, in addition to member capabilities, manages members and join requests and moderates content.",
                "مشرف على المجتمع يمكنه بالإضافة إلى صلاحيات العضو إدارة الأعضاء وطلبات الانضمام والإشراف على المحتوى.",
                ModeratorCapabilities),
        };

        return Task.FromResult(_msg.Ok(roles, "ITEMS_LISTED"));
    }
}
