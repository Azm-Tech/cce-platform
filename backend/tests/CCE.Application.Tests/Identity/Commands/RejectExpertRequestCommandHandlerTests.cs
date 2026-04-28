using CCE.Application.Common.Interfaces;
using CCE.Application.Identity;
using CCE.Application.Identity.Commands.RejectExpertRequest;
using CCE.Domain.Common;
using CCE.Domain.Identity;
using CCE.TestInfrastructure.Time;
using Microsoft.AspNetCore.Identity;

namespace CCE.Application.Tests.Identity.Commands;

public class RejectExpertRequestCommandHandlerTests
{
    [Fact]
    public async Task Throws_KeyNotFound_when_request_missing()
    {
        var service = Substitute.For<IExpertWorkflowService>();
        service.FindIncludingDeletedAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>())
            .Returns((ExpertRegistrationRequest?)null);

        var sut = new RejectExpertRequestCommandHandler(service, BuildDb(), BuildCurrentUser(), new FakeSystemClock());

        var act = async () => await sut.Handle(
            new RejectExpertRequestCommand(System.Guid.NewGuid(), "غير مؤهل", "Insufficient evidence."),
            CancellationToken.None);

        await act.Should().ThrowAsync<System.Collections.Generic.KeyNotFoundException>();
    }

    [Fact]
    public async Task Throws_DomainException_when_actor_unknown()
    {
        var clock = new FakeSystemClock();
        var registration = ExpertRegistrationRequest.Submit(
            System.Guid.NewGuid(), "bio-ar", "bio-en", new[] { "Hydrogen" }, clock);
        var service = Substitute.For<IExpertWorkflowService>();
        service.FindIncludingDeletedAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>())
            .Returns(registration);
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns((System.Guid?)null);

        var sut = new RejectExpertRequestCommandHandler(service, BuildDb(), currentUser, clock);

        var act = async () => await sut.Handle(
            new RejectExpertRequestCommand(registration.Id, "غير مؤهل", "Insufficient evidence."),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Throws_DomainException_when_request_not_pending()
    {
        var clock = new FakeSystemClock();
        var registration = ExpertRegistrationRequest.Submit(
            System.Guid.NewGuid(), "bio-ar", "bio-en", new[] { "Hydrogen" }, clock);
        var adminId = System.Guid.NewGuid();
        registration.Approve(adminId, clock); // already approved — not Pending

        var service = Substitute.For<IExpertWorkflowService>();
        service.FindIncludingDeletedAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>())
            .Returns(registration);

        var sut = new RejectExpertRequestCommandHandler(service, BuildDb(), BuildCurrentUser(adminId), clock);

        var act = async () => await sut.Handle(
            new RejectExpertRequestCommand(registration.Id, "غير مؤهل", "Insufficient evidence."),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Rejects_request_and_persists_when_valid()
    {
        var clock = new FakeSystemClock();
        var requesterId = System.Guid.NewGuid();
        var adminId = System.Guid.NewGuid();
        var registration = ExpertRegistrationRequest.Submit(
            requesterId, "bio-ar", "bio-en", new[] { "Hydrogen", "CCS" }, clock);

        var service = Substitute.For<IExpertWorkflowService>();
        service.FindIncludingDeletedAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>())
            .Returns(registration);

        var users = new[] { BuildUser(requesterId, "alice@cce.local", "alice") };

        var sut = new RejectExpertRequestCommandHandler(service, BuildDb(users), BuildCurrentUser(adminId), clock);

        var dto = await sut.Handle(
            new RejectExpertRequestCommand(registration.Id, "غير مؤهل", "Insufficient evidence."),
            CancellationToken.None);

        dto.Status.Should().Be(ExpertRegistrationStatus.Rejected);
        dto.RejectionReasonEn.Should().Be("Insufficient evidence.");
        dto.RejectionReasonAr.Should().Be("غير مؤهل");
        registration.Status.Should().Be(ExpertRegistrationStatus.Rejected);
        await service.Received(1).SaveAsync(registration, null, Arg.Any<CancellationToken>());
    }

    private static ICurrentUserAccessor BuildCurrentUser(System.Guid? userId = null)
    {
        var stub = Substitute.For<ICurrentUserAccessor>();
        stub.GetUserId().Returns(userId ?? System.Guid.NewGuid());
        return stub;
    }

    private static ICceDbContext BuildDb(IEnumerable<User>? users = null)
    {
        var db = Substitute.For<ICceDbContext>();
        db.Users.Returns((users ?? System.Array.Empty<User>()).AsQueryable());
        db.Roles.Returns(System.Array.Empty<Role>().AsQueryable());
        db.UserRoles.Returns(System.Array.Empty<IdentityUserRole<System.Guid>>().AsQueryable());
        db.StateRepresentativeAssignments.Returns(System.Array.Empty<StateRepresentativeAssignment>().AsQueryable());
        return db;
    }

    private static User BuildUser(System.Guid id, string email, string userName) =>
        new() { Id = id, Email = email, UserName = userName, NormalizedEmail = email.ToUpperInvariant(), NormalizedUserName = userName.ToUpperInvariant() };
}
