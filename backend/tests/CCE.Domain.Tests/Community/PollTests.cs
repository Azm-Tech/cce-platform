using CCE.Domain.Common;
using CCE.Domain.Community;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Community;

public class PollTests
{
    private static Poll NewPoll(FakeSystemClock clock, params string[] options)
        => Poll.Create(System.Guid.NewGuid(), clock.UtcNow.AddDays(1),
            allowMultiple: false, isAnonymous: false, showResultsBeforeClose: true,
            options.Length == 0 ? new[] { "A", "B" } : options, clock);

    [Fact]
    public void Create_builds_options_in_order()
    {
        var poll = NewPoll(new FakeSystemClock(), "Yes", "No", "Maybe");
        poll.Options.Should().HaveCount(3);
        poll.Options.Select(o => o.Label).Should().ContainInOrder("Yes", "No", "Maybe");
    }

    [Fact]
    public void Create_rejects_fewer_than_two_options()
    {
        var clock = new FakeSystemClock();
        var act = () => Poll.Create(System.Guid.NewGuid(), clock.UtcNow.AddDays(1),
            false, false, true, new[] { "Only one" }, clock);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_rejects_past_deadline()
    {
        var clock = new FakeSystemClock();
        var act = () => Poll.Create(System.Guid.NewGuid(), clock.UtcNow.AddMinutes(-1),
            false, false, true, new[] { "A", "B" }, clock);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void IsClosed_after_deadline()
    {
        var clock = new FakeSystemClock();
        var poll = NewPoll(clock);
        poll.IsClosed(clock).Should().BeFalse();
        clock.Advance(System.TimeSpan.FromDays(2));
        poll.IsClosed(clock).Should().BeTrue();
    }

    [Fact]
    public void Incrementing_option_votes_tracks_count()
    {
        var poll = NewPoll(new FakeSystemClock(), "A", "B");
        var first = poll.Options.First();
        first.IncrementVotes();
        first.IncrementVotes();
        first.VoteCount.Should().Be(2);
    }
}
