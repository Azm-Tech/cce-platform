using CCE.Application.Common.Interfaces;
using CCE.Application.Identity.Commands.DeleteUser;
using CCE.Application.Identity.Dtos;
using CCE.Application.Identity.Public;
using CCE.Application.Messages;
using CCE.Domain.Identity;
using NSubstitute;
using static CCE.Application.Tests.Identity.IdentityTestHelpers;

namespace CCE.Application.Tests.Identity.Commands.DeleteUser;

public class DeleteUserCommandHandlerTests
{
    [Fact]
    public async Task Returns_not_found_when_user_does_not_exist()
    {
        var service = Substitute.For<IUserProfileRepository>();
        service.FindAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var db = Substitute.For<ICceDbContext>();
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        var sut = new DeleteUserCommandHandler(db, service, currentUser, BuildMsg());

        var result = await sut.Handle(new DeleteUserCommand(System.Guid.NewGuid()), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Type.Should().Be(Domain.Common.MessageType.NotFound);
    }

    [Fact]
    public async Task Returns_not_found_when_user_already_deleted()
    {
        var userId = System.Guid.NewGuid();
        var user = BuildUser(userId, "a@b.c", "a");
        user.SoftDelete(System.Guid.NewGuid(), System.DateTimeOffset.UtcNow);

        var service = Substitute.For<IUserProfileRepository>();
        service.FindAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var db = Substitute.For<ICceDbContext>();
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        var sut = new DeleteUserCommandHandler(db, service, currentUser, BuildMsg());

        var result = await sut.Handle(new DeleteUserCommand(userId), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Type.Should().Be(Domain.Common.MessageType.NotFound);
    }

    [Fact]
    public async Task Soft_deletes_user_and_returns_detail()
    {
        var userId = System.Guid.NewGuid();
        var actorId = System.Guid.NewGuid();
        var user = BuildUser(userId, "a@b.c", "a");

        var service = Substitute.For<IUserProfileRepository>();
        service.FindAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var db = Substitute.For<ICceDbContext>();
        db.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns(actorId);

        var sut = new DeleteUserCommandHandler(db, service, currentUser, BuildMsg());

        var result = await sut.Handle(new DeleteUserCommand(userId), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Id.Should().Be(userId);
        user.IsDeleted.Should().BeTrue();
        user.DeletedById.Should().Be(actorId);
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
