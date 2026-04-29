using CCE.Application.Content.Commands.CreateNews;

namespace CCE.Application.Tests.Content.Commands;

public class CreateNewsCommandValidatorTests
{
    private static CreateNewsCommand ValidCmd() => new(
        "خبر", "News", "محتوى", "Content", "first-post", null);

    [Fact]
    public void Valid_command_passes()
    {
        var sut = new CreateNewsCommandValidator();

        var result = sut.Validate(ValidCmd());

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "News")]
    [InlineData("خبر", "")]
    public void Empty_titles_are_rejected(string titleAr, string titleEn)
    {
        var sut = new CreateNewsCommandValidator();
        var cmd = ValidCmd() with { TitleAr = titleAr, TitleEn = titleEn };

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Empty_slug_is_rejected()
    {
        var sut = new CreateNewsCommandValidator();
        var cmd = ValidCmd() with { Slug = "" };

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateNewsCommand.Slug));
    }
}
