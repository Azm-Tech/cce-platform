using CCE.Domain.Common;
using CCE.Domain.Identity;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Identity;

public class StateRepresentativeAssignmentTests
{
    private static FakeSystemClock NewClock() => new();

    [Fact]
    public void Assign_factory_sets_required_fields()
    {
        var clock = NewClock();
        var userId = System.Guid.NewGuid();
        var countryId = System.Guid.NewGuid();
        var adminId = System.Guid.NewGuid();

        var a = StateRepresentativeAssignment.Assign(userId, countryId, adminId, clock);

        a.Id.Should().NotBe(System.Guid.Empty);
        a.UserId.Should().Be(userId);
        a.CountryId.Should().Be(countryId);
        a.AssignedById.Should().Be(adminId);
        a.AssignedOn.Should().Be(clock.UtcNow);
        a.RevokedOn.Should().BeNull();
        a.RevokedById.Should().BeNull();
        a.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Assign_with_empty_userId_throws()
    {
        var clock = NewClock();
        var act = () => StateRepresentativeAssignment.Assign(System.Guid.Empty, System.Guid.NewGuid(), System.Guid.NewGuid(), clock);
        act.Should().Throw<DomainException>().WithMessage("*UserId*");
    }

    [Fact]
    public void Assign_with_empty_countryId_throws()
    {
        var clock = NewClock();
        var act = () => StateRepresentativeAssignment.Assign(System.Guid.NewGuid(), System.Guid.Empty, System.Guid.NewGuid(), clock);
        act.Should().Throw<DomainException>().WithMessage("*CountryId*");
    }

    [Fact]
    public void Assign_with_empty_assignedById_throws()
    {
        var clock = NewClock();
        var act = () => StateRepresentativeAssignment.Assign(System.Guid.NewGuid(), System.Guid.NewGuid(), System.Guid.Empty, clock);
        act.Should().Throw<DomainException>().WithMessage("*AssignedById*");
    }

    [Fact]
    public void Revoke_sets_revoke_fields_and_marks_deleted()
    {
        var clock = NewClock();
        var a = StateRepresentativeAssignment.Assign(System.Guid.NewGuid(), System.Guid.NewGuid(), System.Guid.NewGuid(), clock);
        clock.Advance(System.TimeSpan.FromHours(2));
        var revoker = System.Guid.NewGuid();

        a.Revoke(revoker, clock);

        a.RevokedOn.Should().Be(clock.UtcNow);
        a.RevokedById.Should().Be(revoker);
        a.IsDeleted.Should().BeTrue();
        a.DeletedOn.Should().Be(clock.UtcNow);
        a.DeletedById.Should().Be(revoker);
    }

    [Fact]
    public void Revoking_already_revoked_assignment_throws()
    {
        var clock = NewClock();
        var a = StateRepresentativeAssignment.Assign(System.Guid.NewGuid(), System.Guid.NewGuid(), System.Guid.NewGuid(), clock);
        a.Revoke(System.Guid.NewGuid(), clock);
        clock.Advance(System.TimeSpan.FromMinutes(5));

        var act = () => a.Revoke(System.Guid.NewGuid(), clock);
        act.Should().Throw<DomainException>().WithMessage("*already revoked*");
    }
}
