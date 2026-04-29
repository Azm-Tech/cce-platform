using CCE.Application.Content.Commands.CreateEvent;

namespace CCE.Application.Tests.Content.Commands;

public class CreateEventCommandValidatorTests
{
    private static readonly System.DateTimeOffset StartsOn =
        new(2026, 9, 1, 9, 0, 0, System.TimeSpan.Zero);

    private static readonly System.DateTimeOffset EndsOn =
        new(2026, 9, 1, 17, 0, 0, System.TimeSpan.Zero);

    private static CreateEventCommand ValidCmd() => new(
        "حدث", "Event", "وصف", "Description",
        StartsOn, EndsOn,
        null, null, null, null);

    [Fact]
    public void Valid_command_passes()
    {
        var sut = new CreateEventCommandValidator();

        var result = sut.Validate(ValidCmd());

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "Event")]
    [InlineData("حدث", "")]
    public void Empty_titles_are_rejected(string titleAr, string titleEn)
    {
        var sut = new CreateEventCommandValidator();
        var cmd = ValidCmd() with { TitleAr = titleAr, TitleEn = titleEn };

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void EndsOn_not_after_StartsOn_is_rejected()
    {
        var sut = new CreateEventCommandValidator();
        // EndsOn == StartsOn (not strictly after)
        var cmd = ValidCmd() with { EndsOn = StartsOn };

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateEventCommand.EndsOn));
    }
}
