using CCE.Application.Common.Interfaces;
using CCE.Application.Identity.Queries.ListExpertProfiles;
using CCE.Domain.Identity;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Identity.Queries;

public class ListExpertProfilesQueryHandlerTests
{
    [Fact]
    public async Task Returns_empty_paged_result_when_no_profiles_exist()
    {
        var db = BuildDb(System.Array.Empty<ExpertProfile>(), System.Array.Empty<User>());
        var sut = new ListExpertProfilesQueryHandler(db, IdentityTestHelpers.BuildMsg());

        var result = await sut.Handle(new ListExpertProfilesQuery(Page: 1, PageSize: 20), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().BeEmpty();
        result.Data.Total.Should().Be(0);
        result.Data.Page.Should().Be(1);
        result.Data.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Returns_profiles_with_user_names_populated()
    {
        var clock = new FakeSystemClock();
        var aliceId = System.Guid.NewGuid();
        var adminId = System.Guid.NewGuid();

        var aliceProfile = BuildProfile(aliceId, adminId, "سيرة أليس", "Alice Bio", new[] { "energy", "solar" }, "أستاذة", "Professor", clock);

        var users = new[]
        {
            BuildUser(aliceId, "alice@cce.local", "alice"),
        };

        var db = BuildDb(new[] { aliceProfile }, users);
        var sut = new ListExpertProfilesQueryHandler(db, IdentityTestHelpers.BuildMsg());

        var result = await sut.Handle(new ListExpertProfilesQuery(Page: 1, PageSize: 20), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Total.Should().Be(1);
        result.Data.Items.Should().HaveCount(1);

        var item = result.Data.Items.Single();
        item.UserId.Should().Be(aliceId);
        item.UserName.Should().Be("alice");
        item.BioEn.Should().Be("Alice Bio");
        item.ExpertiseTags.Should().BeEquivalentTo(new[] { "energy", "solar" });
        item.AcademicTitleEn.Should().Be("Professor");
        item.ApprovedById.Should().Be(adminId);
    }

    [Fact]
    public async Task Search_filter_restricts_results_to_matching_user_name_or_email()
    {
        var clock = new FakeSystemClock();
        var aliceId = System.Guid.NewGuid();
        var bobId = System.Guid.NewGuid();
        var adminId = System.Guid.NewGuid();

        var aliceProfile = BuildProfile(aliceId, adminId, "سيرة أليس", "Alice Bio", new[] { "energy" }, "أستاذة", "Professor", clock);
        var bobProfile = BuildProfile(bobId, adminId, "سيرة بوب", "Bob Bio", new[] { "wind" }, "دكتور", "Doctor", clock);

        var users = new[]
        {
            BuildUser(aliceId, "alice@cce.local", "alice"),
            BuildUser(bobId, "bob@cce.local", "bob"),
        };

        var db = BuildDb(new[] { aliceProfile, bobProfile }, users);
        var sut = new ListExpertProfilesQueryHandler(db, IdentityTestHelpers.BuildMsg());

        var result = await sut.Handle(
            new ListExpertProfilesQuery(Search: "alice"),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Total.Should().Be(1);
        result.Data.Items.Single().UserId.Should().Be(aliceId);
    }

    private static ExpertProfile BuildProfile(
        System.Guid userId,
        System.Guid adminId,
        string bioAr,
        string bioEn,
        string[] tags,
        string titleAr,
        string titleEn,
        FakeSystemClock clock)
    {
        var request = ExpertRegistrationRequest.Submit(userId, bioAr, bioEn, tags, System.Guid.NewGuid(), clock);
        request.Approve(adminId, clock);
        return ExpertProfile.CreateFromApprovedRequest(request, titleAr, titleEn, clock);
    }

    private static ICceDbContext BuildDb(
        IEnumerable<ExpertProfile> profiles,
        IEnumerable<User> users)
    {
        var db = Substitute.For<ICceDbContext>();
        db.ExpertProfiles.Returns(profiles.AsQueryable());
        db.Users.Returns(users.AsQueryable());
        return db;
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
