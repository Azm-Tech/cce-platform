using System.Collections.Generic;
using CCE.Application.Common;
using CCE.Application.Community.Public.Dtos;
using MediatR;

namespace CCE.Application.Community.Public.Queries.GetMentionableUsers;

public sealed record GetMentionableUsersQuery(
    System.Guid CommunityId,
    string Q,
    int Limit = 10) : IRequest<Response<IReadOnlyList<MentionableUserDto>>>;
