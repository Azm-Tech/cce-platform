using CCE.Infrastructure.Identity;
using CCE.Infrastructure.Tests.Migration;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Xunit;

namespace CCE.Infrastructure.Tests.Identity;

[Collection(nameof(EntraIdCollection))]
public sealed class EntraIdRegistrationTests : IClassFixture<MigratorFixture>
{
    private readonly EntraIdFixture _entra;
    private readonly MigratorFixture _migrator;

    public EntraIdRegistrationTests(EntraIdFixture entra, MigratorFixture migrator)
    {
        _entra = entra;
        _migrator = migrator;
    }

    [Fact]
    public async Task CreateUserAsync_HappyPath_CreatesGraphUserAndPersistsCceUser()
    {
        _entra.Reset();
        _entra.StubCreateUserSuccess();

        var dbSuffix = $"reg_happy_{Guid.NewGuid():N}";
        await using var ctx = _migrator.CreateContextWithFreshDb(dbSuffix);
        await ctx.Database.EnsureDeletedAsync();
        await ctx.Database.MigrateAsync();

        var service = BuildService(ctx);
        var dto = new RegistrationRequest("Test", "Newuser", "test.newuser@cce.local", "test.newuser");

        var result = await service.CreateUserAsync(dto, CancellationToken.None);

        result.EntraIdObjectId.Should().Be(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        result.UserPrincipalName.Should().Be("test.newuser@cce.local");
        result.TemporaryPassword.Should().NotBeNullOrWhiteSpace();

        // CCE-side persistence check.
        await using var verifyCtx = _migrator.CreateContextWithFreshDb(dbSuffix);
        var persisted = await verifyCtx.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.EntraIdObjectId == result.EntraIdObjectId);
        persisted.Should().NotBeNull();
        persisted!.Email.Should().Be("test.newuser@cce.local");
        persisted.EmailConfirmed.Should().BeFalse();
    }

    [Fact]
    public async Task CreateUserAsync_UpnConflict_ThrowsConflictExceptionAndDoesNotPersist()
    {
        _entra.Reset();
        _entra.StubCreateUserConflict();

        var dbSuffix = $"reg_conflict_{Guid.NewGuid():N}";
        await using var ctx = _migrator.CreateContextWithFreshDb(dbSuffix);
        await ctx.Database.EnsureDeletedAsync();
        await ctx.Database.MigrateAsync();

        var service = BuildService(ctx);
        var dto = new RegistrationRequest("Dup", "Licate", "dup.licate@cce.local", "dup.licate");

        var act = () => service.CreateUserAsync(dto, CancellationToken.None);
        await act.Should().ThrowAsync<EntraIdRegistrationConflictException>();

        // No CCE row created.
        await using var verifyCtx = _migrator.CreateContextWithFreshDb(dbSuffix);
        var count = await verifyCtx.Users.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task CreateUserAsync_InsufficientPrivileges_ThrowsAuthorizationExceptionAndDoesNotPersist()
    {
        _entra.Reset();
        _entra.StubCreateUserForbidden();

        var dbSuffix = $"reg_forbidden_{Guid.NewGuid():N}";
        await using var ctx = _migrator.CreateContextWithFreshDb(dbSuffix);
        await ctx.Database.EnsureDeletedAsync();
        await ctx.Database.MigrateAsync();

        var service = BuildService(ctx);
        var dto = new RegistrationRequest("No", "Privs", "no.privs@cce.local", "no.privs");

        var act = () => service.CreateUserAsync(dto, CancellationToken.None);
        await act.Should().ThrowAsync<EntraIdRegistrationAuthorizationException>();

        await using var verifyCtx = _migrator.CreateContextWithFreshDb(dbSuffix);
        var count = await verifyCtx.Users.CountAsync();
        count.Should().Be(0);
    }

    private EntraIdRegistrationService BuildService(CCE.Infrastructure.Persistence.CceDbContext ctx)
    {
        var options = Options.Create(new EntraIdOptions
        {
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret",
            GraphTenantId = "test-tenant-id",
            GraphTenantDomain = "cce.local",
        });
        var fakeFactory = new FakeGraphClientFactory(_entra, options);
        return new EntraIdRegistrationService(fakeFactory, ctx, NullLogger<EntraIdRegistrationService>.Instance);
    }

    private sealed class FakeGraphClientFactory : EntraIdGraphClientFactory
    {
        private readonly EntraIdFixture _fixture;
        public FakeGraphClientFactory(EntraIdFixture fixture, IOptions<EntraIdOptions> opts) : base(opts)
            => _fixture = fixture;
        public override GraphServiceClient Create() => _fixture.CreateGraphClient();
    }
}
