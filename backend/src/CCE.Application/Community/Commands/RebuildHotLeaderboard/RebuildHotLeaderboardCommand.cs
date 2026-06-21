using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Community.Commands.RebuildHotLeaderboard;

/// <summary>
/// Admin recovery command: rebuilds the <c>hot:{communityId}</c> Redis sorted-set from SQL scores.
/// Pass <c>null</c> to rebuild every community at once.
/// This is offline repair only — it must never be triggered by runtime events.
/// </summary>
public sealed record RebuildHotLeaderboardCommand(Guid? CommunityId)
    : IRequest<Response<VoidData>>;
