using CCE.Domain.Common;
using CCE.Domain.Community;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Community;

public class CommunityTests
{
    private static CCE.Domain.Community.Community NewCommunity(CommunityVisibility v = CommunityVisibility.Public)
        => CCE.Domain.Community.Community.Create("اسم", "Name", "وصف", "Desc", "my-community", v);

    [Fact]
    public void Create_defaults_to_active_with_zero_members()
    {
        var c = NewCommunity();
        c.IsActive.Should().BeTrue();
        c.MemberCount.Should().Be(0);
        c.IsPublic.Should().BeTrue();
    }

    [Fact]
    public void Create_rejects_non_kebab_slug()
    {
        var act = () => CCE.Domain.Community.Community.Create("ا", "N", "و", "D", "Not Kebab", CommunityVisibility.Public);
        act.Should().Throw<DomainException>().WithMessage("*kebab*");
    }

    [Fact]
    public void Member_count_increments_and_never_goes_negative()
    {
        var c = NewCommunity();
        c.IncrementMembers();
        c.IncrementMembers();
        c.MemberCount.Should().Be(2);
        c.DecrementMembers();
        c.DecrementMembers();
        c.DecrementMembers();
        c.MemberCount.Should().Be(0);
    }

    [Fact]
    public void ChangeVisibility_updates_flag()
    {
        var c = NewCommunity();
        c.ChangeVisibility(CommunityVisibility.Private);
        c.IsPublic.Should().BeFalse();
    }
}

public class CommunityJoinRequestTests
{
    private static FakeSystemClock Clock() => new();

    [Fact]
    public void Submit_starts_pending()
    {
        var r = CommunityJoinRequest.Submit(System.Guid.NewGuid(), System.Guid.NewGuid(), Clock());
        r.Status.Should().Be(JoinRequestStatus.Pending);
    }

    [Fact]
    public void Approve_then_second_decision_throws()
    {
        var clock = Clock();
        var r = CommunityJoinRequest.Submit(System.Guid.NewGuid(), System.Guid.NewGuid(), clock);
        r.Approve(System.Guid.NewGuid(), clock);
        r.Status.Should().Be(JoinRequestStatus.Approved);
        r.DecidedOn.Should().NotBeNull();

        var act = () => r.Reject(System.Guid.NewGuid(), clock);
        act.Should().Throw<DomainException>().WithMessage("*pending*");
    }
}
