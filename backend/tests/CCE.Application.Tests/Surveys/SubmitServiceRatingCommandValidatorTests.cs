using CCE.Application.Surveys.Commands.SubmitServiceRating;

namespace CCE.Application.Tests.Surveys;

public class SubmitServiceRatingCommandValidatorTests
{
    private readonly SubmitServiceRatingCommandValidator _sut = new();

    [Fact]
    public void Valid_command_passes()
    {
        var result = _sut.Validate(new SubmitServiceRatingCommand(5, null, null, "/home", "en"));
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Rating_out_of_range_fails(int rating)
    {
        var result = _sut.Validate(new SubmitServiceRatingCommand(rating, null, null, "/home", "en"));
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void Valid_ratings_pass(int rating)
    {
        var result = _sut.Validate(new SubmitServiceRatingCommand(rating, null, null, "/home", "en"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_page_fails()
    {
        var result = _sut.Validate(new SubmitServiceRatingCommand(3, null, null, "", "en"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Invalid_locale_fails()
    {
        var result = _sut.Validate(new SubmitServiceRatingCommand(3, null, null, "/home", "fr"));
        result.IsValid.Should().BeFalse();
    }
}
