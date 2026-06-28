using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace CCE.Api.Common.SignalR;

/// <summary>
/// Routes SignalR messages by the JWT <c>sub</c> claim so that
/// <c>Clients.User(userId)</c> matches the CCE user identifier. Shared by both the
/// External and Internal API hubs (Option 2: shared Redis backplane).
/// </summary>
public sealed class SubClaimUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User?.FindFirstValue("sub")
            ?? connection.User?.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}