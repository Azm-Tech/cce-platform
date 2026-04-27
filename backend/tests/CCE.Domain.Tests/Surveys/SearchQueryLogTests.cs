using CCE.Domain.Common;
using CCE.Domain.Surveys;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Surveys;

public class SearchQueryLogTests
{
    [Fact]
    public void Record_search() {
        var log = SearchQueryLog.Record(null, "carbon capture", 47, 120, "en", new FakeSystemClock());
        log.QueryText.Should().Be("carbon capture");
        log.ResultsCount.Should().Be(47);
    }

    [Fact]
    public void Empty_queryText_throws() {
        var act = () => SearchQueryLog.Record(null, "", 0, 100, "ar", new FakeSystemClock());
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Negative_resultsCount_throws() {
        var act = () => SearchQueryLog.Record(null, "x", -1, 100, "ar", new FakeSystemClock());
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Negative_responseTime_throws() {
        var act = () => SearchQueryLog.Record(null, "x", 0, -1, "ar", new FakeSystemClock());
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void SearchQueryLog_is_NOT_audited() {
        typeof(SearchQueryLog).GetCustomAttributes(typeof(AuditedAttribute), inherit: false)
            .Should().BeEmpty();
    }
}
