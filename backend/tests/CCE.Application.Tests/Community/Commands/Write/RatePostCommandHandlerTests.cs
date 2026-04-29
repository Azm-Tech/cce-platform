using CCE.Application.Common.Interfaces;
using CCE.Application.Community;
using CCE.Application.Community.Commands.RatePost;
using CCE.Domain.Common;
using CCE.Domain.Community;

namespace CCE.Application.Tests.Community.Commands.Write;

public class RatePostCommandHandlerTests
{
    private static ISystemClock MakeClock()
    {
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(System.DateTimeOffset.UtcNow);
        return clock;
    }

    private static Post MakePost(ISystemClock clock)
        => Post.Create(System.Guid.NewGuid(), System.Guid.NewGuid(), "Content", "en", false, clock);

    [Fact]
    public async Task Saves_rating_for_valid_stars()
    {
        var clock = MakeClock();
        var post = MakePost(clock);
        var userId = System.Guid.NewGuid();

        var service = Substitute.For<ICommunityWriteService>();
        service.FindPostAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns(userId);

        var sut = new RatePostCommandHandler(service, currentUser, clock);

        await sut.Handle(new RatePostCommand(post.Id, 4), CancellationToken.None);

        await service.Received(1).SaveRatingAsync(
            Arg.Is<PostRating>(r => r.PostId == post.Id && r.UserId == userId && r.Stars == 4),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_KeyNotFoundException_when_post_missing()
    {
        var clock = MakeClock();
        var service = Substitute.For<ICommunityWriteService>();
        service.FindPostAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>()).Returns((Post?)null);
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns(System.Guid.NewGuid());

        var sut = new RatePostCommandHandler(service, currentUser, clock);

        var act = async () => await sut.Handle(
            new RatePostCommand(System.Guid.NewGuid(), 3), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Throws_DomainException_for_invalid_stars()
    {
        var clock = MakeClock();
        var post = MakePost(clock);
        var service = Substitute.For<ICommunityWriteService>();
        service.FindPostAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns(System.Guid.NewGuid());

        var sut = new RatePostCommandHandler(service, currentUser, clock);

        var act = async () => await sut.Handle(
            new RatePostCommand(post.Id, 6), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Throws_DomainException_when_no_user()
    {
        var clock = MakeClock();
        var service = Substitute.For<ICommunityWriteService>();
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns((System.Guid?)null);

        var sut = new RatePostCommandHandler(service, currentUser, clock);

        var act = async () => await sut.Handle(
            new RatePostCommand(System.Guid.NewGuid(), 3), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}
