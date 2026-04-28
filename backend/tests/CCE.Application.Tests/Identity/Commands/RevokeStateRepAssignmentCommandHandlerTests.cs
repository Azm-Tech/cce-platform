using CCE.Application.Common.Interfaces;
using CCE.Application.Identity;
using CCE.Application.Identity.Commands.RevokeStateRepAssignment;
using CCE.Domain.Common;
using CCE.Domain.Identity;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Identity.Commands;

public class RevokeStateRepAssignmentCommandHandlerTests
{
    [Fact]
    public async Task Throws_KeyNotFound_when_assignment_missing()
    {
        var service = Substitute.For<IStateRepAssignmentService>();
        service.FindIncludingRevokedAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>())
            .Returns((StateRepresentativeAssignment?)null);

        var sut = new RevokeStateRepAssignmentCommandHandler(service, BuildCurrentUser(), new FakeSystemClock());

        var act = async () => await sut.Handle(new RevokeStateRepAssignmentCommand(System.Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<System.Collections.Generic.KeyNotFoundException>();
    }

    [Fact]
    public async Task Throws_DomainException_when_actor_unknown()
    {
        var clock = new FakeSystemClock();
        var assignment = StateRepresentativeAssignment.Assign(
            System.Guid.NewGuid(), System.Guid.NewGuid(), System.Guid.NewGuid(), clock);

        var service = Substitute.For<IStateRepAssignmentService>();
        service.FindIncludingRevokedAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>())
            .Returns(assignment);
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns((System.Guid?)null);

        var sut = new RevokeStateRepAssignmentCommandHandler(service, currentUser, clock);

        var act = async () => await sut.Handle(new RevokeStateRepAssignmentCommand(assignment.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Throws_DomainException_when_already_revoked()
    {
        var clock = new FakeSystemClock();
        var revokerId = System.Guid.NewGuid();
        var assignment = StateRepresentativeAssignment.Assign(
            System.Guid.NewGuid(), System.Guid.NewGuid(), revokerId, clock);
        assignment.Revoke(revokerId, clock); // already revoked

        var service = Substitute.For<IStateRepAssignmentService>();
        service.FindIncludingRevokedAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>())
            .Returns(assignment);

        var sut = new RevokeStateRepAssignmentCommandHandler(service, BuildCurrentUser(revokerId), clock);

        var act = async () => await sut.Handle(new RevokeStateRepAssignmentCommand(assignment.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Revokes_and_persists_when_valid()
    {
        var clock = new FakeSystemClock();
        var revokerId = System.Guid.NewGuid();
        var assignment = StateRepresentativeAssignment.Assign(
            System.Guid.NewGuid(), System.Guid.NewGuid(), revokerId, clock);

        var service = Substitute.For<IStateRepAssignmentService>();
        service.FindIncludingRevokedAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>())
            .Returns(assignment);

        var sut = new RevokeStateRepAssignmentCommandHandler(service, BuildCurrentUser(revokerId), clock);

        await sut.Handle(new RevokeStateRepAssignmentCommand(assignment.Id), CancellationToken.None);

        assignment.IsDeleted.Should().BeTrue();
        assignment.RevokedOn.Should().NotBeNull();
        assignment.RevokedById.Should().Be(revokerId);
        await service.Received(1).UpdateAsync(assignment, Arg.Any<CancellationToken>());
    }

    private static ICurrentUserAccessor BuildCurrentUser(System.Guid? userId = null)
    {
        var stub = Substitute.For<ICurrentUserAccessor>();
        stub.GetUserId().Returns(userId ?? System.Guid.NewGuid());
        return stub;
    }
}
