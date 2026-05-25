using CCE.Application.Common.Interfaces;
using CCE.Application.Identity.Public;
using CCE.Application.Identity.Public.Commands.SubmitExpertRequest;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Identity;
using CCE.TestInfrastructure.Time;
using static CCE.Application.Tests.Identity.IdentityTestHelpers;

namespace CCE.Application.Tests.Identity.Public.Commands;

public class SubmitExpertRequestCommandHandlerTests
{
    [Fact]
    public async Task Persists_request_and_returns_dto()
    {
        var clock = new FakeSystemClock();
        var db = Substitute.For<ICceDbContext>();
        var service = Substitute.For<IExpertRequestSubmissionRepository>();
        var sut = new SubmitExpertRequestCommandHandler(db, service, clock, BuildMsg());

        var requesterId = System.Guid.NewGuid();
        var cmd = new SubmitExpertRequestCommand(
            requesterId,
            "سيرة ذاتية",
            "English bio",
            new[] { "Hydrogen", "Solar" });

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.RequestedById.Should().Be(requesterId);
        result.Data.RequestedBioAr.Should().Be("سيرة ذاتية");
        result.Data.RequestedBioEn.Should().Be("English bio");
        result.Data.RequestedTags.Should().BeEquivalentTo(new[] { "Hydrogen", "Solar" });
        result.Data.Status.Should().Be(ExpertRegistrationStatus.Pending);
        result.Data.ProcessedOn.Should().BeNull();
        await service.Received(1).AddAsync(Arg.Any<ExpertRegistrationRequest>(), Arg.Any<CancellationToken>());
        await db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Domain_throws_when_bio_is_empty()
    {
        var clock = new FakeSystemClock();
        var db = Substitute.For<ICceDbContext>();
        var service = Substitute.For<IExpertRequestSubmissionRepository>();
        var sut = new SubmitExpertRequestCommandHandler(db, service, clock, BuildMsg());

        var cmd = new SubmitExpertRequestCommand(
            System.Guid.NewGuid(),
            "",
            "English bio",
            System.Array.Empty<string>());

        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
        await service.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await db.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }
}
