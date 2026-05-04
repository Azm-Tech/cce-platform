using System.Security.Claims;
using CCE.Api.Common.Auth;
using CCE.Domain.Identity;
using CCE.Infrastructure.Tests.Migration;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CCE.Infrastructure.Tests.Identity;

[Collection(nameof(MigratorCollection))]
public sealed class EntraIdObjectIdLazyResolutionTests
{
    private readonly MigratorFixture _fixture;

    public EntraIdObjectIdLazyResolutionTests(MigratorFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task EnsureLinkedAsync_ExistingUserMatchedByEmail_LinksObjectId()
    {
        var dbSuffix = $"resolver_link_{Guid.NewGuid():N}";
        await using var ctx = _fixture.CreateContextWithFreshDb(dbSuffix);
        await ctx.Database.EnsureDeletedAsync();
        await ctx.Database.MigrateAsync();

        // Seed an existing User row.
        var seedUserId = Guid.NewGuid();
        var seedEmail = "alice@cce.local";
        ctx.Users.Add(new User
        {
            Id = seedUserId,
            UserName = seedEmail,
            NormalizedUserName = seedEmail.ToUpperInvariant(),
            Email = seedEmail,
            NormalizedEmail = seedEmail.ToUpperInvariant(),
            EmailConfirmed = true,
        });
        await ctx.SaveChangesAsync();

        var resolver = new EntraIdUserResolver(ctx, NullLogger<EntraIdUserResolver>.Instance);
        var objectId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim("oid", objectId.ToString()),
                new Claim("preferred_username", seedEmail),
            },
            authenticationType: "test"));

        await resolver.EnsureLinkedAsync(principal);

        // Re-read to verify the link persisted.
        await using var verifyCtx = _fixture.CreateContextWithFreshDb(dbSuffix);
        var linked = await verifyCtx.Users.AsNoTracking().FirstAsync(u => u.Id == seedUserId);
        linked.EntraIdObjectId.Should().Be(objectId);
    }

    [Fact]
    public async Task EnsureLinkedAsync_NoMatchingUser_CreatesStubFromEntraId()
    {
        var dbSuffix = $"resolver_stub_{Guid.NewGuid():N}";
        await using var ctx = _fixture.CreateContextWithFreshDb(dbSuffix);
        await ctx.Database.EnsureDeletedAsync();
        await ctx.Database.MigrateAsync();

        var resolver = new EntraIdUserResolver(ctx, NullLogger<EntraIdUserResolver>.Instance);
        var objectId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var partnerUpn = "bob@partner.example";

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim("oid", objectId.ToString()),
                new Claim("preferred_username", partnerUpn),
                new Claim("name", "Bob Partner"),
            },
            authenticationType: "test"));

        await resolver.EnsureLinkedAsync(principal);

        // Re-read; a stub User should exist linked to the Entra ID objectId.
        await using var verifyCtx = _fixture.CreateContextWithFreshDb(dbSuffix);
        var stub = await verifyCtx.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.EntraIdObjectId == objectId);
        stub.Should().NotBeNull();
        stub!.Email.Should().Be(partnerUpn);
        stub.UserName.Should().Be(partnerUpn);
        stub.EmailConfirmed.Should().BeFalse("stub users must be confirmed by an admin before role assignment");
    }

    [Fact]
    public async Task EnsureLinkedAsync_AlreadyLinked_NoOp()
    {
        var dbSuffix = $"resolver_noop_{Guid.NewGuid():N}";
        await using var ctx = _fixture.CreateContextWithFreshDb(dbSuffix);
        await ctx.Database.EnsureDeletedAsync();
        await ctx.Database.MigrateAsync();

        var seedUserId = Guid.NewGuid();
        var seedEmail = "carol@cce.local";
        var objectId = Guid.Parse("00000000-0000-0000-0000-000000000003");

        // Seed a User already linked to the objectId.
        var seedUser = new User
        {
            Id = seedUserId,
            UserName = seedEmail,
            NormalizedUserName = seedEmail.ToUpperInvariant(),
            Email = seedEmail,
            NormalizedEmail = seedEmail.ToUpperInvariant(),
            EmailConfirmed = true,
        };
        seedUser.LinkEntraIdObjectId(objectId);
        ctx.Users.Add(seedUser);
        await ctx.SaveChangesAsync();

        var beforeCount = await ctx.Users.CountAsync();

        // Detach so we can verify the resolver doesn't write through cached state.
        ctx.Entry(seedUser).State = EntityState.Detached;

        var resolver = new EntraIdUserResolver(ctx, NullLogger<EntraIdUserResolver>.Instance);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim("oid", objectId.ToString()),
                new Claim("preferred_username", seedEmail),
            },
            authenticationType: "test"));

        await resolver.EnsureLinkedAsync(principal);

        // Verify no new User row created (was 1, still 1) — no-op.
        await using var verifyCtx = _fixture.CreateContextWithFreshDb(dbSuffix);
        var afterCount = await verifyCtx.Users.CountAsync();
        afterCount.Should().Be(beforeCount);
    }
}
