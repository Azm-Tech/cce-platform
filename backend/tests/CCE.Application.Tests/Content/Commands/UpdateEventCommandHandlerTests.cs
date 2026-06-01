using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Commands.UpdateEvent;
using CCE.Application.Localization;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Commands;

public class UpdateEventCommandHandlerTests
{
    private static readonly System.DateTimeOffset StartsOn =
        new(2026, 9, 1, 9, 0, 0, System.TimeSpan.Zero);

    private static readonly System.DateTimeOffset EndsOn =
        new(2026, 9, 1, 17, 0, 0, System.TimeSpan.Zero);

    [Fact]
    public async Task Returns_not_found_when_event_missing()
    {
        var (sut, _, _) = BuildSut(null, System.Guid.NewGuid());

        var result = await sut.Handle(BuildCommand(System.Guid.NewGuid()), CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Updates_content_and_saves()
    {
        var clock = new FakeSystemClock();
        var topicId = System.Guid.NewGuid();
        var ev = Event.Schedule(
            "old-ar", "old-en", "old-desc-ar", "old-desc-en",
            StartsOn, EndsOn, null, null, null, null, topicId, clock);

        var (sut, db, repo) = BuildSut(ev, topicId);

        var cmd = new UpdateEventCommand(
            ev.Id,
            "new-ar", "new-en", "new-desc-ar", "new-desc-en",
            "الرياض", "Riyadh",
            null, null,
            topicId);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.TitleEn.Should().Be("new-en");
        result.Data.DescriptionAr.Should().Be("new-desc-ar");
        await db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static UpdateEventCommand BuildCommand(System.Guid id) =>
        new(id, "ar", "en", "desc-ar", "desc-en", null, null, null, null, System.Guid.NewGuid());

    private static (UpdateEventCommandHandler sut, ICceDbContext db, IRepository<Event, System.Guid> repo) BuildSut(Event? evToReturn, System.Guid topicId)
    {
        var repo = Substitute.For<IRepository<Event, System.Guid>>();
        repo.GetByIdAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>()).Returns(evToReturn);
        var db = Substitute.For<ICceDbContext>();
        var topic = CCE.Domain.Community.Topic.Create(
            "name-ar", "name-en", "desc-ar", "desc-en", "slug", null, null, 0);
        typeof(CCE.Domain.Community.Topic).GetProperty(nameof(CCE.Domain.Community.Topic.Id))!
            .SetValue(topic, topicId);
        db.Topics.Returns(new[] { topic }.AsQueryable());
        var localization = Substitute.For<ILocalizationService>();
        localization.GetString(Arg.Any<string>(), Arg.Any<string?>()).Returns(call => call.ArgAt<string>(0));
        return (new UpdateEventCommandHandler(repo, db, new MessageFactory(localization)), db, repo);
    }
}
