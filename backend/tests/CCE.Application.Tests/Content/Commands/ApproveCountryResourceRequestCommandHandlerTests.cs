using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Commands.ApproveCountryResourceRequest;
using CCE.Application.Tests.Notifications;
using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.Domain.Country;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Commands;

public class ApproveCountryResourceRequestCommandHandlerTests
{
    [Fact]
    public async Task Returns_not_found_when_request_missing()
    {
        var repo = Substitute.For<IRepository<CountryContentRequest, System.Guid>>();
        repo.GetByIdAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>())
            .Returns((CountryContentRequest?)null);

        var sut = BuildSut(repo, BuildCurrentUser());

        var result = await sut.Handle(
            new ApproveCountryResourceRequestCommand(System.Guid.NewGuid(), null, null),
            CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Throws_DomainException_when_actor_unknown()
    {
        var clock = new FakeSystemClock();
        var entity = BuildPendingRequest(clock);

        var repo = Substitute.For<IRepository<CountryContentRequest, System.Guid>>();
        repo.GetByIdAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>())
            .Returns(entity);

        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns((System.Guid?)null);

        var sut = BuildSut(repo, currentUser, clock);

        var act = async () => await sut.Handle(
            new ApproveCountryResourceRequestCommand(entity.Id, null, null),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Approves_request_and_returns_ok_response()
    {
        var clock = new FakeSystemClock();
        var entity = BuildPendingRequest(clock);

        var repo = Substitute.For<IRepository<CountryContentRequest, System.Guid>>();
        repo.GetByIdAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>())
            .Returns(entity);

        var db = Substitute.For<ICceDbContext>();
        var sut = BuildSut(repo, BuildCurrentUser(), clock, db);

        var response = await sut.Handle(
            new ApproveCountryResourceRequestCommand(entity.Id, "ملاحظات", "Notes"),
            CancellationToken.None);

        response.Success.Should().BeTrue();
        response.Data!.Status.Should().Be(CountryContentRequestStatus.Approved);
        response.Data.Kind.Should().Be(ContentKind.Resource);
        response.Data.AdminNotesAr.Should().Be("ملاحظات");
        await db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static CountryContentRequest BuildPendingRequest(FakeSystemClock clock) =>
        CountryContentRequest.SubmitResource(
            System.Guid.NewGuid(), System.Guid.NewGuid(),
            "عنوان", "Title", "وصف", "Description",
            ResourceType.Paper, System.Guid.NewGuid(), clock);

    private static ICurrentUserAccessor BuildCurrentUser(System.Guid? userId = null)
    {
        var stub = Substitute.For<ICurrentUserAccessor>();
        stub.GetUserId().Returns(userId ?? System.Guid.NewGuid());
        return stub;
    }

    private static ApproveCountryResourceRequestCommandHandler BuildSut(
        IRepository<CountryContentRequest, System.Guid> repo,
        ICurrentUserAccessor currentUser,
        FakeSystemClock? clock = null,
        ICceDbContext? db = null) =>
        new(repo,
            db ?? Substitute.For<ICceDbContext>(),
            currentUser,
            clock ?? new FakeSystemClock(),
            NotificationTestMessages.Create());
}
