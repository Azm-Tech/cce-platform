using CCE.Application.Common.Interfaces;
using CCE.Application.Content;
using CCE.Application.Content.Commands.ApproveCountryResourceRequest;
using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.Domain.Country;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Commands;

public class ApproveCountryResourceRequestCommandHandlerTests
{
    [Fact]
    public async Task Throws_KeyNotFound_when_request_missing()
    {
        var service = Substitute.For<ICountryResourceRequestService>();
        service.FindIncludingDeletedAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>())
            .Returns((CountryResourceRequest?)null);

        var sut = BuildSut(service, BuildCurrentUser());

        var act = async () => await sut.Handle(
            new ApproveCountryResourceRequestCommand(System.Guid.NewGuid(), null, null),
            CancellationToken.None);

        await act.Should().ThrowAsync<System.Collections.Generic.KeyNotFoundException>();
    }

    [Fact]
    public async Task Throws_DomainException_when_actor_unknown()
    {
        var clock = new FakeSystemClock();
        var entity = BuildPendingRequest(clock);

        var service = Substitute.For<ICountryResourceRequestService>();
        service.FindIncludingDeletedAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>())
            .Returns(entity);

        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns((System.Guid?)null);

        var sut = BuildSut(service, currentUser, clock);

        var act = async () => await sut.Handle(
            new ApproveCountryResourceRequestCommand(entity.Id, null, null),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Approves_request_and_returns_dto_when_valid()
    {
        var clock = new FakeSystemClock();
        var adminId = System.Guid.NewGuid();
        var entity = BuildPendingRequest(clock);

        var service = Substitute.For<ICountryResourceRequestService>();
        service.FindIncludingDeletedAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>())
            .Returns(entity);

        var sut = BuildSut(service, BuildCurrentUser(adminId), clock);

        var dto = await sut.Handle(
            new ApproveCountryResourceRequestCommand(entity.Id, "ملاحظات", "Notes"),
            CancellationToken.None);

        entity.Status.Should().Be(CountryResourceRequestStatus.Approved);
        dto.Status.Should().Be(CountryResourceRequestStatus.Approved);
        dto.AdminNotesAr.Should().Be("ملاحظات");
        dto.AdminNotesEn.Should().Be("Notes");
        await service.Received(1).UpdateAsync(entity, Arg.Any<CancellationToken>());
    }

    private static CountryResourceRequest BuildPendingRequest(FakeSystemClock clock) =>
        CountryResourceRequest.Submit(
            System.Guid.NewGuid(), System.Guid.NewGuid(),
            "عنوان", "Title",
            "وصف", "Description",
            ResourceType.Pdf, System.Guid.NewGuid(), clock);

    private static ICurrentUserAccessor BuildCurrentUser(System.Guid? userId = null)
    {
        var stub = Substitute.For<ICurrentUserAccessor>();
        stub.GetUserId().Returns(userId ?? System.Guid.NewGuid());
        return stub;
    }

    private static ApproveCountryResourceRequestCommandHandler BuildSut(
        ICountryResourceRequestService service,
        ICurrentUserAccessor currentUser,
        FakeSystemClock? clock = null) =>
        new(service, currentUser, clock ?? new FakeSystemClock());
}
