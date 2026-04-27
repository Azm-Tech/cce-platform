using CCE.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace CCE.Domain.Identity;

/// <summary>
/// CCE role — thin specialization of <see cref="IdentityRole{TKey}"/> with <see cref="System.Guid"/>
/// keys. The seeded role names are listed in <c>backend/permissions.yaml</c> and reflected in
/// <c>CCE.Domain.RolePermissionMap</c>: SuperAdmin, ContentManager, StateRepresentative,
/// CommunityExpert, RegisteredUser. (<c>Anonymous</c> is NOT seeded — it's an implicit role
/// representing unauthenticated callers.)
/// </summary>
[Audited]
public class Role : IdentityRole<System.Guid>
{
    public Role() { }

    public Role(string roleName) : base(roleName) { }
}
