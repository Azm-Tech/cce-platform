using CCE.Application.Search.Queries;

namespace CCE.Application.Tests.Search.Queries;

public class SearchQueryValidatorTests
{
    private readonly SearchQueryValidator _sut = new();

    [Fact]
    public void Valid_query_passes()
    {
        var result = _sut.Validate(new SearchQuery("carbon", null, 1, 20));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_q_fails()
    {
        var result = _sut.Validate(new SearchQuery(string.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(SearchQuery.Q));
    }

    [Fact]
    public void Q_too_short_fails()
    {
        var result = _sut.Validate(new SearchQuery("a"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(SearchQuery.Q));
    }

    [Fact]
    public void PageSize_over_100_fails()
    {
        var result = _sut.Validate(new SearchQuery("carbon", null, 1, 101));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(SearchQuery.PageSize));
    }
}
