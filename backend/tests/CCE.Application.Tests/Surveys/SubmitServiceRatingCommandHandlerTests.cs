using CCE.Application.Common.Interfaces;
using CCE.Application.Surveys;
using CCE.Application.Surveys.Commands.SubmitServiceRating;
using CCE.Domain.Common;
using CCE.Domain.Surveys;

namespace CCE.Application.Tests.Surveys;

public class SubmitServiceRatingCommandHandlerTests
{
    [Fact]
    public async Task Returns_new_guid_id()
    {
        var service = Substitute.For<IServiceRatingService>();
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(System.DateTimeOffset.UtcNow);
        currentUser.GetUserId().Returns((System.Guid?)null);

        var sut = new SubmitServiceRatingCommandHandler(service, currentUser, clock);
        var cmd = new SubmitServiceRatingCommand(5, null, null, "/home", "en");

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBe(System.Guid.Empty);
    }

    [Fact]
    public async Task Calls_service_save_with_rating()
    {
        var service = Substitute.For<IServiceRatingService>();
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(System.DateTimeOffset.UtcNow);
        currentUser.GetUserId().Returns((System.Guid?)null);

        ServiceRating? captured = null;
        await service.SaveAsync(Arg.Do<ServiceRating>(r => captured = r), Arg.Any<CancellationToken>());

        var sut = new SubmitServiceRatingCommandHandler(service, currentUser, clock);
        var cmd = new SubmitServiceRatingCommand(4, "تعليق", "comment", "/news", "ar");

        await sut.Handle(cmd, CancellationToken.None);

        await service.Received(1).SaveAsync(Arg.Any<ServiceRating>(), CancellationToken.None);
    }

    [Fact]
    public async Task Passes_optional_user_id_when_authenticated()
    {
        var userId = System.Guid.NewGuid();
        var service = Substitute.For<IServiceRatingService>();
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(System.DateTimeOffset.UtcNow);
        currentUser.GetUserId().Returns(userId);

        var sut = new SubmitServiceRatingCommandHandler(service, currentUser, clock);
        await sut.Handle(new SubmitServiceRatingCommand(3, null, null, "/page", "en"), CancellationToken.None);

        await service.Received(1).SaveAsync(
            Arg.Is<ServiceRating>(r => r.UserId == userId),
            CancellationToken.None);
    }
}
