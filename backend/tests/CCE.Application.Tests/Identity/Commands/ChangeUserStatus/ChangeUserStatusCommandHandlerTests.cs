using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Identity.Commands.ChangeUserStatus;
using CCE.Application.Identity.Dtos;
using CCE.Application.Identity.Public;
using CCE.Application.Identity.Queries.GetUserById;
using CCE.Application.Messages;
using CCE.Domain.Identity;
using MediatR;
using NSubstitute;
using static CCE.Application.Tests.Identity.IdentityTestHelpers;

namespace CCE.Application.Tests.Identity.Commands.ChangeUserStatus;

public class ChangeUserStatusCommandHandlerTests
{
    [Fact]
    public async Task Returns_not_found_when_user_does_not_exist()
    {
        var service = Substitute.For<IUserProfileRepository>();
        service.FindAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var db = Substitute.For<ICceDbContext>();
        var mediator = Substitute.For<IMediator>();
        var sut = new ChangeUserStatusCommandHandler(db, service, mediator, BuildMsg());

        var result = await sut.Handle(new ChangeUserStatusCommand(System.Guid.NewGuid(), true), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Type.Should().Be(Domain.Common.MessageType.NotFound);
    }

    [Fact]
    public async Task Activate_sets_status_to_active_and_returns_user_detail()
    {
        var userId = System.Guid.NewGuid();
        var user = BuildUser(userId, "a@b.c", "a");

        var service = Substitute.For<IUserProfileRepository>();
        service.FindAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);

        var db = Substitute.For<ICceDbContext>();
        db.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var expectedDto = new UserDetailDto(
            userId, "a@b.c", "a", "ar", KnowledgeLevel.Beginner,
            new List<string>(), null, null, Array.Empty<string>(), true);

        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetUserByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Response<UserDetailDto>.Ok(expectedDto, "SUCCESS_OPERATION", ""));

        var sut = new ChangeUserStatusCommandHandler(db, service, mediator, BuildMsg());

        var result = await sut.Handle(new ChangeUserStatusCommand(userId, true), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.IsActive.Should().BeTrue();
        user.Status.Should().Be(UserStatus.Active);
        service.Received(1).Update(user);
        await db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deactivate_sets_status_to_inactive_and_returns_user_detail()
    {
        var userId = System.Guid.NewGuid();
        var user = BuildUser(userId, "a@b.c", "a");

        var service = Substitute.For<IUserProfileRepository>();
        service.FindAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);

        var db = Substitute.For<ICceDbContext>();
        db.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var expectedDto = new UserDetailDto(
            userId, "a@b.c", "a", "ar", KnowledgeLevel.Beginner,
            new List<string>(), null, null, Array.Empty<string>(), false);

        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetUserByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Response<UserDetailDto>.Ok(expectedDto, "SUCCESS_OPERATION", ""));

        var sut = new ChangeUserStatusCommandHandler(db, service, mediator, BuildMsg());

        var result = await sut.Handle(new ChangeUserStatusCommand(userId, false), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.IsActive.Should().BeFalse();
        user.Status.Should().Be(UserStatus.Inactive);
        service.Received(1).Update(user);
        await db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static User BuildUser(System.Guid id, string email, string userName) =>
        new()
        {
            Id = id,
            Email = email,
            UserName = userName,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = userName.ToUpperInvariant(),
        };
}
