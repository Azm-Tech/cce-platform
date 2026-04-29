using CCE.Application.Content.Commands.CreatePage;
using CCE.Domain.Content;

namespace CCE.Application.Tests.Content.Commands;

public class CreatePageCommandValidatorTests
{
    private static CreatePageCommand ValidCmd() => new(
        "test-slug", PageType.Custom, "ar", "en", "content-ar", "content-en");

    [Fact]
    public void Valid_command_passes()
    {
        var sut = new CreatePageCommandValidator();

        var result = sut.Validate(ValidCmd());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_slug_is_rejected()
    {
        var sut = new CreatePageCommandValidator();
        var cmd = ValidCmd() with { Slug = "" };

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePageCommand.Slug));
    }

    [Theory]
    [InlineData("", "en")]
    [InlineData("ar", "")]
    public void Empty_title_is_rejected(string titleAr, string titleEn)
    {
        var sut = new CreatePageCommandValidator();
        var cmd = ValidCmd() with { TitleAr = titleAr, TitleEn = titleEn };

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
    }
}
