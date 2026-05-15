using CCE.Application.Common.Interfaces;
using CCE.Application.Identity.Public.Queries.GetMyExpertStatus;
using CCE.Domain.Identity;
using CCE.TestInfrastructure.Time;
using static CCE.Application.Tests.Identity.IdentityTestHelpers;

namespace CCE.Application.Tests.Identity.Public.Queries;

public class GetMyExpertStatusQueryHandlerTests
{
    [Fact]
    public async Task Returns_null_when_no_request_exists()
    {
        var db = BuildDb(System.Array.Empty<ExpertRegistrationRequest>());
        var sut = new GetMyExpertStatusQueryHandler(db, BuildErrors());

        var result = await sut.Handle(new GetMyExpertStatusQuery(System.Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("IDENTITY_EXPERT_REQUEST_NOT_FOUND");
    }

    [Fact]
    public async Task Returns_dto_when_request_exists()
    {
        var clock = new FakeSystemClock();
        var userId = System.Guid.NewGuid();
        var request = ExpertRegistrationRequest.Submit(userId, "سيرة", "Bio", new[] { "Wind" }, clock);

        var db = BuildDb(new[] { request });
        var sut = new GetMyExpertStatusQueryHandler(db, BuildErrors());

        var result = await sut.Handle(new GetMyExpertStatusQuery(userId), CancellationToken.None);

        result.Should().NotBeNull();
        result.Data!.RequestedById.Should().Be(userId);
        result.Data.RequestedBioAr.Should().Be("سيرة");
        result.Data.RequestedBioEn.Should().Be("Bio");
        result.Data.RequestedTags.Should().BeEquivalentTo(new[] { "Wind" });
        result.Data.Status.Should().Be(ExpertRegistrationStatus.Pending);
    }

    [Fact]
    public async Task Returns_latest_when_multiple_requests_exist()
    {
        var clock = new FakeSystemClock();
        var userId = System.Guid.NewGuid();
        var older = ExpertRegistrationRequest.Submit(userId, "قديمة", "Older bio", new[] { "Solar" }, clock);
        clock.Advance(System.TimeSpan.FromDays(1));
        var newer = ExpertRegistrationRequest.Submit(userId, "أحدث", "Newer bio", new[] { "Wind" }, clock);

        var db = BuildDb(new[] { older, newer });
        var sut = new GetMyExpertStatusQueryHandler(db, BuildErrors());

        var result = await sut.Handle(new GetMyExpertStatusQuery(userId), CancellationToken.None);

        result.Should().NotBeNull();
        result.Data!.RequestedBioEn.Should().Be("Newer bio");
    }

    private static ICceDbContext BuildDb(IEnumerable<ExpertRegistrationRequest> requests)
    {
        var db = Substitute.For<ICceDbContext>();
        db.ExpertRegistrationRequests.Returns(requests.AsQueryable());
        return db;
    }
}
