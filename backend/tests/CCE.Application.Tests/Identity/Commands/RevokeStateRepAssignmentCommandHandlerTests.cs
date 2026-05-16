using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Identity;
using CCE.Application.Identity.Commands.RevokeStateRepAssignment;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Identity;
using CCE.TestInfrastructure.Time;
using static CCE.Application.Tests.Identity.IdentityTestHelpers;

namespace CCE.Application.Tests.Identity.Commands;

public class RevokeStateRepAssignmentCommandHandlerTests
{
    [Fact]
    public async Task Returns_failure_when_assignment_missing()
    {
        var db = Substitute.For<ICceDbContext>();
        var service = Substitute.For<IStateRepAssignmentRepository>();
        service.FindIncludingRevokedAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>())
            .Returns((StateRepresentativeAssignment?)null);

        var sut = new RevokeStateRepAssignmentCommandHandler(db, service, BuildCurrentUser(), new FakeSystemClock(), BuildMsg());

        var result = await sut.Handle(new RevokeStateRepAssignmentCommand(System.Guid.NewGuid()), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Code.Should().Be(SystemCode.ERR003);
    }

    [Fact]
    public async Task Returns_failure_when_actor_unknown()
    {
        var clock = new FakeSystemClock();
        var assignment = StateRepresentativeAssignment.Assign(
            System.Guid.NewGuid(), System.Guid.NewGuid(), System.Guid.NewGuid(), clock);

        var db = Substitute.For<ICceDbContext>();
        var service = Substitute.For<IStateRepAssignmentRepository>();
        service.FindIncludingRevokedAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>())
            .Returns(assignment);
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns((System.Guid?)null);

        var sut = new RevokeStateRepAssignmentCommandHandler(db, service, currentUser, clock, BuildMsg());

        var result = await sut.Handle(new RevokeStateRepAssignmentCommand(assignment.Id), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Code.Should().Be(SystemCode.ERR028);
    }

    [Fact]
    public async Task Throws_DomainException_when_already_revoked()
    {
        var clock = new FakeSystemClock();
        var revokerId = System.Guid.NewGuid();
        var assignment = StateRepresentativeAssignment.Assign(
            System.Guid.NewGuid(), System.Guid.NewGuid(), revokerId, clock);
        assignment.Revoke(revokerId, clock); // already revoked

        var db = Substitute.For<ICceDbContext>();
        var service = Substitute.For<IStateRepAssignmentRepository>();
        service.FindIncludingRevokedAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>())
            .Returns(assignment);

        var sut = new RevokeStateRepAssignmentCommandHandler(db, service, BuildCurrentUser(revokerId), clock, BuildMsg());

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

        var db = Substitute.For<ICceDbContext>();
        var service = Substitute.For<IStateRepAssignmentRepository>();
        service.FindIncludingRevokedAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>())
            .Returns(assignment);

        var sut = new RevokeStateRepAssignmentCommandHandler(db, service, BuildCurrentUser(revokerId), clock, BuildMsg());

        var result = await sut.Handle(new RevokeStateRepAssignmentCommand(assignment.Id), CancellationToken.None);

        result.Success.Should().BeTrue();
        assignment.IsDeleted.Should().BeTrue();
        assignment.RevokedOn.Should().NotBeNull();
        assignment.RevokedById.Should().Be(revokerId);
        service.Received(1).Update(assignment);
        await db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static ICurrentUserAccessor BuildCurrentUser(System.Guid? userId = null)
    {
        var stub = Substitute.For<ICurrentUserAccessor>();
        stub.GetUserId().Returns(userId ?? System.Guid.NewGuid());
        return stub;
    }
}
