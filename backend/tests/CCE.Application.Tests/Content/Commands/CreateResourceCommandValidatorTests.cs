using CCE.Application.Content.Commands.CreateResource;
using CCE.Domain.Content;

namespace CCE.Application.Tests.Content.Commands;

public class CreateResourceCommandValidatorTests
{
    private static CreateResourceCommand ValidCmd() => new(
        "عنوان", "Title",
        "وصف", "Description",
        ResourceType.Pdf,
        System.Guid.NewGuid(),
        null,
        System.Guid.NewGuid());

    [Fact]
    public void Valid_command_passes()
    {
        var sut = new CreateResourceCommandValidator();

        var result = sut.Validate(ValidCmd());

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "Title")]
    [InlineData("عنوان", "")]
    public void Empty_titles_are_rejected(string titleAr, string titleEn)
    {
        var sut = new CreateResourceCommandValidator();
        var cmd = ValidCmd() with { TitleAr = titleAr, TitleEn = titleEn };

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Empty_CategoryId_is_rejected()
    {
        var sut = new CreateResourceCommandValidator();
        var cmd = ValidCmd() with { CategoryId = System.Guid.Empty };

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateResourceCommand.CategoryId));
    }
}
