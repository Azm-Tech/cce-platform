using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Commands.CreateEvent;
using CCE.Application.Localization;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Commands;

public class CreateEventCommandHandlerTests
{
    private static readonly System.Guid TopicId = System.Guid.NewGuid();
    private static readonly System.DateTimeOffset StartsOn =
        new(2026, 9, 1, 9, 0, 0, System.TimeSpan.Zero);

    private static readonly System.DateTimeOffset EndsOn =
        new(2026, 9, 1, 17, 0, 0, System.TimeSpan.Zero);

    [Fact]
    public async Task Persists_event_when_inputs_valid()
    {
        var (sut, repo, db) = BuildSut(TopicId);

        var result = await sut.Handle(BuildCmd(TopicId), CancellationToken.None);

        result.Success.Should().BeTrue();
        await repo.Received(1).AddAsync(Arg.Any<Event>(), Arg.Any<CancellationToken>());
        await db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Returns_dto_with_correct_fields()
    {
        var (sut, _, _) = BuildSut(TopicId);

        var result = await sut.Handle(BuildCmd(TopicId), CancellationToken.None);

        result.Data!.TitleAr.Should().Be("حدث");
        result.Data.TitleEn.Should().Be("Event");
        result.Data.StartsOn.Should().Be(StartsOn);
        result.Data.EndsOn.Should().Be(EndsOn);
        result.Data.ICalUid.Should().EndWith("@cce.moenergy.gov.sa");
    }

    private static CreateEventCommand BuildCmd(System.Guid topicId) =>
        new("حدث", "Event", "وصف", "Description", StartsOn, EndsOn,
            null, null, null, null, topicId);

    private static (CreateEventCommandHandler sut,
        IRepository<Event, System.Guid> repo,
        ICceDbContext db) BuildSut(System.Guid topicId)
    {
        var repo = Substitute.For<IRepository<Event, System.Guid>>();
        var db = Substitute.For<ICceDbContext>();
        var topic = CCE.Domain.Community.Topic.Create(
            "name-ar", "name-en", "desc-ar", "desc-en", "slug", null, null, 0);
        typeof(CCE.Domain.Community.Topic).GetProperty(nameof(CCE.Domain.Community.Topic.Id))!
            .SetValue(topic, topicId);
        db.Topics.Returns(new[] { topic }.AsQueryable());
        var localization = Substitute.For<ILocalizationService>();
        localization.GetString(Arg.Any<string>(), Arg.Any<string?>()).Returns(call => call.ArgAt<string>(0));
        var sut = new CreateEventCommandHandler(repo, db, new FakeSystemClock(), new MessageFactory(localization));
        return (sut, repo, db);
    }
}
