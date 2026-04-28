using CCE.Application.Identity;
using CCE.Application.Identity.Commands.AssignUserRoles;
using CCE.Application.Identity.Dtos;
using CCE.Application.Identity.Queries.GetUserById;
using CCE.Domain.Identity;
using MediatR;

namespace CCE.Application.Tests.Identity.Commands;

public class AssignUserRolesCommandHandlerTests
{
    [Fact]
    public async Task Returns_null_when_service_reports_user_missing()
    {
        var service = Substitute.For<IUserRoleAssignmentService>();
        service.ReplaceRolesAsync(Arg.Any<System.Guid>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(false);
        var mediator = Substitute.For<IMediator>();
        var sut = new AssignUserRolesCommandHandler(service, mediator);

        var result = await sut.Handle(new AssignUserRolesCommand(System.Guid.NewGuid(), new[] { "SuperAdmin" }), CancellationToken.None);

        result.Should().BeNull();
        await mediator.DidNotReceiveWithAnyArgs().Send<UserDetailDto?>(default!, default);
    }

    [Fact]
    public async Task Returns_user_detail_when_service_succeeds()
    {
        var id = System.Guid.NewGuid();
        var service = Substitute.For<IUserRoleAssignmentService>();
        service.ReplaceRolesAsync(id, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var dto = new UserDetailDto(id, "alice@cce.local", "alice", "ar",
            KnowledgeLevel.Beginner, System.Array.Empty<string>(), null, null,
            new[] { "ContentManager" }, true);
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Is<GetUserByIdQuery>(q => q.Id == id), Arg.Any<CancellationToken>())
            .Returns((UserDetailDto?)dto);

        var sut = new AssignUserRolesCommandHandler(service, mediator);

        var result = await sut.Handle(new AssignUserRolesCommand(id, new[] { "ContentManager" }), CancellationToken.None);

        result.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task Forwards_role_list_to_service()
    {
        var id = System.Guid.NewGuid();
        var service = Substitute.For<IUserRoleAssignmentService>();
        service.ReplaceRolesAsync(default, default!, default).ReturnsForAnyArgs(true);
        var mediator = Substitute.For<IMediator>();
        var sut = new AssignUserRolesCommandHandler(service, mediator);

        var roles = new[] { "SuperAdmin", "ContentManager" };
        await sut.Handle(new AssignUserRolesCommand(id, roles), CancellationToken.None);

        await service.Received(1).ReplaceRolesAsync(
            id,
            Arg.Is<IReadOnlyCollection<string>>(r => r.SequenceEqual(roles)),
            Arg.Any<CancellationToken>());
    }
}
