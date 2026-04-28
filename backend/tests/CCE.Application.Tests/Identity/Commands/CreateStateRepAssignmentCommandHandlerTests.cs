using CCE.Application.Common.Interfaces;
using CCE.Application.Identity;
using CCE.Application.Identity.Commands.CreateStateRepAssignment;
using CCE.Domain.Common;
using CCE.Domain.Country;
using CCE.Domain.Identity;
using CCE.TestInfrastructure.Time;
using Microsoft.AspNetCore.Identity;

namespace CCE.Application.Tests.Identity.Commands;

public class CreateStateRepAssignmentCommandHandlerTests
{
    [Fact]
    public async Task Throws_KeyNotFound_when_user_missing()
    {
        var db = BuildDb(System.Array.Empty<User>(), System.Array.Empty<Country>());
        var sut = new CreateStateRepAssignmentCommandHandler(
            db, Substitute.For<IStateRepAssignmentService>(), BuildCurrentUser(), new FakeSystemClock());

        var act = async () => await sut.Handle(
            new CreateStateRepAssignmentCommand(System.Guid.NewGuid(), System.Guid.NewGuid()),
            CancellationToken.None);

        await act.Should().ThrowAsync<System.Collections.Generic.KeyNotFoundException>();
    }

    [Fact]
    public async Task Throws_KeyNotFound_when_country_missing()
    {
        var aliceId = System.Guid.NewGuid();
        var users = new[] { BuildUser(aliceId, "alice@cce.local", "alice") };
        var db = BuildDb(users, System.Array.Empty<Country>());
        var sut = new CreateStateRepAssignmentCommandHandler(
            db, Substitute.For<IStateRepAssignmentService>(), BuildCurrentUser(), new FakeSystemClock());

        var act = async () => await sut.Handle(
            new CreateStateRepAssignmentCommand(aliceId, System.Guid.NewGuid()),
            CancellationToken.None);

        await act.Should().ThrowAsync<System.Collections.Generic.KeyNotFoundException>();
    }

    [Fact]
    public async Task Throws_DomainException_when_actor_unknown()
    {
        var aliceId = System.Guid.NewGuid();
        var country = BuildCountry();
        var users = new[] { BuildUser(aliceId, "alice@cce.local", "alice") };
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns((System.Guid?)null);

        var db = BuildDb(users, new[] { country });
        var sut = new CreateStateRepAssignmentCommandHandler(
            db, Substitute.For<IStateRepAssignmentService>(), currentUser, new FakeSystemClock());

        var act = async () => await sut.Handle(
            new CreateStateRepAssignmentCommand(aliceId, country.Id),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Persists_assignment_and_returns_dto_when_inputs_valid()
    {
        var aliceId = System.Guid.NewGuid();
        var country = BuildCountry();
        var users = new[] { BuildUser(aliceId, "alice@cce.local", "alice") };
        var service = Substitute.For<IStateRepAssignmentService>();
        var currentUser = BuildCurrentUser();
        var clock = new FakeSystemClock();

        var db = BuildDb(users, new[] { country });
        var sut = new CreateStateRepAssignmentCommandHandler(db, service, currentUser, clock);

        var dto = await sut.Handle(
            new CreateStateRepAssignmentCommand(aliceId, country.Id),
            CancellationToken.None);

        dto.UserId.Should().Be(aliceId);
        dto.CountryId.Should().Be(country.Id);
        dto.UserName.Should().Be("alice");
        dto.IsActive.Should().BeTrue();
        await service.Received(1).SaveAsync(Arg.Any<StateRepresentativeAssignment>(), Arg.Any<CancellationToken>());
    }

    private static ICurrentUserAccessor BuildCurrentUser(System.Guid? userId = null)
    {
        var stub = Substitute.For<ICurrentUserAccessor>();
        stub.GetUserId().Returns(userId ?? System.Guid.NewGuid());
        return stub;
    }

    private static ICceDbContext BuildDb(IEnumerable<User> users, IEnumerable<Country> countries)
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

    private static Country BuildCountry()
    {
        var instance = (Country)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Country));
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
