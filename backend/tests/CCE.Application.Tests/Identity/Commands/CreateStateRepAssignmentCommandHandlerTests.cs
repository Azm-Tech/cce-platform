using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Identity;
using CCE.Application.Identity.Commands.CreateStateRepAssignment;
using CCE.Domain.Common;
using CCE.Domain.Identity;
using CCE.TestInfrastructure.Time;
using Microsoft.AspNetCore.Identity;
using static CCE.Application.Tests.Identity.IdentityTestHelpers;

namespace CCE.Application.Tests.Identity.Commands;

public class CreateStateRepAssignmentCommandHandlerTests
{
    [Fact]
    public async Task Returns_failure_when_user_missing()
    {
        var db = BuildDb(System.Array.Empty<User>(), System.Array.Empty<CCE.Domain.Country.Country>());
        var sut = new CreateStateRepAssignmentCommandHandler(
            db, Substitute.For<IStateRepAssignmentRepository>(), BuildCurrentUser(), new FakeSystemClock(), BuildErrors());

        var result = await sut.Handle(
            new CreateStateRepAssignmentCommand(System.Guid.NewGuid(), System.Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("IDENTITY_USER_NOT_FOUND");
    }

    [Fact]
    public async Task Returns_failure_when_country_missing()
    {
        var aliceId = System.Guid.NewGuid();
        var users = new[] { BuildUser(aliceId, "alice@cce.local", "alice") };
        var db = BuildDb(users, System.Array.Empty<CCE.Domain.Country.Country>());
        var sut = new CreateStateRepAssignmentCommandHandler(
            db, Substitute.For<IStateRepAssignmentRepository>(), BuildCurrentUser(), new FakeSystemClock(), BuildErrors());

        var result = await sut.Handle(
            new CreateStateRepAssignmentCommand(aliceId, System.Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("COUNTRY_COUNTRY_NOT_FOUND");
    }

    [Fact]
    public async Task Returns_failure_when_actor_unknown()
    {
        var aliceId = System.Guid.NewGuid();
        var country = BuildCountry();
        var users = new[] { BuildUser(aliceId, "alice@cce.local", "alice") };
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns((System.Guid?)null);

        var db = BuildDb(users, new[] { country });
        var sut = new CreateStateRepAssignmentCommandHandler(
            db, Substitute.For<IStateRepAssignmentRepository>(), currentUser, new FakeSystemClock(), BuildErrors());

        var result = await sut.Handle(
            new CreateStateRepAssignmentCommand(aliceId, country.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("IDENTITY_NOT_AUTHENTICATED");
    }

    [Fact]
    public async Task Persists_assignment_and_returns_dto_when_inputs_valid()
    {
        var aliceId = System.Guid.NewGuid();
        var country = BuildCountry();
        var users = new[] { BuildUser(aliceId, "alice@cce.local", "alice") };
        var service = Substitute.For<IStateRepAssignmentRepository>();
        var currentUser = BuildCurrentUser();
        var clock = new FakeSystemClock();

        var db = BuildDb(users, new[] { country });
        var sut = new CreateStateRepAssignmentCommandHandler(db, service, currentUser, clock, BuildErrors());

        var result = await sut.Handle(
            new CreateStateRepAssignmentCommand(aliceId, country.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.UserId.Should().Be(aliceId);
        result.Data!.CountryId.Should().Be(country.Id);
        result.Data!.UserName.Should().Be("alice");
        result.Data!.IsActive.Should().BeTrue();
        await service.Received(1).SaveAsync(Arg.Any<StateRepresentativeAssignment>(), Arg.Any<CancellationToken>());
    }

    private static ICurrentUserAccessor BuildCurrentUser(System.Guid? userId = null)
    {
        var stub = Substitute.For<ICurrentUserAccessor>();
        stub.GetUserId().Returns(userId ?? System.Guid.NewGuid());
        return stub;
    }

    private static ICceDbContext BuildDb(IEnumerable<User> users, IEnumerable<CCE.Domain.Country.Country> countries)
    {
        var db = Substitute.For<ICceDbContext>();
        db.Users.Returns(users.AsQueryable());
        db.Roles.Returns(System.Array.Empty<Role>().AsQueryable());
        db.UserRoles.Returns(System.Array.Empty<IdentityUserRole<System.Guid>>().AsQueryable());
        db.StateRepresentativeAssignments.Returns(System.Array.Empty<StateRepresentativeAssignment>().AsQueryable());
        db.Countries.Returns(countries.AsQueryable());
        return db;
    }

    private static User BuildUser(System.Guid id, string email, string userName) =>
        new() { Id = id, Email = email, UserName = userName, NormalizedEmail = email.ToUpperInvariant(), NormalizedUserName = userName.ToUpperInvariant() };

    private static CCE.Domain.Country.Country BuildCountry()
    {
        var instance = (CCE.Domain.Country.Country)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(CCE.Domain.Country.Country));
        // Initialize the backing _domainEvents field (inline-initialized in Entity<T> ctor, skipped by GetUninitializedObject).
        var eventsField = typeof(CCE.Domain.Common.Entity<System.Guid>)
            .GetField("_domainEvents", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        eventsField?.SetValue(instance, new System.Collections.Generic.List<CCE.Domain.Common.IDomainEvent>());
        // Set the Id via the protected setter.
        var idProp = typeof(CCE.Domain.Common.Entity<System.Guid>).GetProperty("Id");
        idProp?.GetSetMethod(nonPublic: true)?.Invoke(instance, new object[] { System.Guid.NewGuid() });
        return instance;
    }
}
