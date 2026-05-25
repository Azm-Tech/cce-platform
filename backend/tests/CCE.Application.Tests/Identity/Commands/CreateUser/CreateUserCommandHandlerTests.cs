using CCE.Application.Common;
using CCE.Application.Identity.Auth.Common;
using CCE.Application.Identity.Commands.CreateUser;
using CCE.Application.Identity.Dtos;
using CCE.Application.Identity.Queries.GetUserById;
using CCE.Application.Messages;
using CCE.Domain.Identity;
using MediatR;
using NSubstitute;
using static CCE.Application.Tests.Identity.IdentityTestHelpers;

namespace CCE.Application.Tests.Identity.Commands.CreateUser;

public class CreateUserCommandHandlerTests
{
    [Fact]
    public async Task Returns_conflict_when_email_already_exists()
    {
        var auth = Substitute.For<IAuthService>();
        auth.AdminCreateUserAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid?>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new AdminCreateResult(null, true, false));

        var mediator = Substitute.For<IMediator>();
        var sut = new CreateUserCommandHandler(auth, mediator, BuildMsg());

        var result = await sut.Handle(
            new CreateUserCommand("A", "B", "a@b.c", "pass1234", "123", null, "cce-admin"),
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Type.Should().Be(Domain.Common.MessageType.Conflict);
    }

    [Fact]
    public async Task Returns_business_rule_on_creation_failure()
    {
        var auth = Substitute.For<IAuthService>();
        auth.AdminCreateUserAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid?>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new AdminCreateResult(null, false, true));

        var mediator = Substitute.For<IMediator>();
        var sut = new CreateUserCommandHandler(auth, mediator, BuildMsg());

        var result = await sut.Handle(
            new CreateUserCommand("A", "B", "a@b.c", "pass1234", "123", null, "cce-admin"),
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Type.Should().Be(Domain.Common.MessageType.BusinessRule);
    }

    [Fact]
    public async Task Creates_user_and_returns_detail()
    {
        var userId = System.Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "a@b.c",
            UserName = "a@b.c",
            NormalizedEmail = "A@B.C",
            NormalizedUserName = "A@B.C",
        };

        var auth = Substitute.For<IAuthService>();
        auth.AdminCreateUserAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid?>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new AdminCreateResult(user, false, false));

        var expectedDto = new UserDetailDto(
            userId, "a@b.c", "a@b.c", "ar", KnowledgeLevel.Beginner,
            new List<string>(), null, null, new[] { "cce-admin" }, true);

        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetUserByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Response<UserDetailDto>.Ok(expectedDto, "SUCCESS_OPERATION", ""));

        var sut = new CreateUserCommandHandler(auth, mediator, BuildMsg());

        var result = await sut.Handle(
            new CreateUserCommand("A", "B", "a@b.c", "pass1234", "123", null, "cce-admin"),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Id.Should().Be(userId);
    }
}
