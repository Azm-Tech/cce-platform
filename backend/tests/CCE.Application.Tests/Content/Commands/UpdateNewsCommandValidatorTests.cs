using CCE.Application.Content.Commands.UpdateNews;

namespace CCE.Application.Tests.Content.Commands;

public class UpdateNewsCommandValidatorTests
{
    private static UpdateNewsCommand ValidCmd() => new(
        System.Guid.NewGuid(),
        "خبر", "News", "محتوى", "Content",
        "first-post", null,
        new byte[8]);

    [Fact]
    public void Valid_command_passes()
    {
        var sut = new UpdateNewsCommandValidator();

        var result = sut.Validate(ValidCmd());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_Id_is_rejected()
    {
        var sut = new UpdateNewsCommandValidator();
        var cmd = ValidCmd() with { Id = System.Guid.Empty };

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateNewsCommand.Id));
    }

    [Fact]
    public void RowVersion_wrong_length_is_rejected()
    {
        var sut = new UpdateNewsCommandValidator();
        var cmd = ValidCmd() with { RowVersion = new byte[4] };

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateNewsCommand.RowVersion));
    }

    [Fact]
    public void Slug_not_kebab_case_is_rejected()
    {
        var sut = new UpdateNewsCommandValidator();
        var cmd = ValidCmd() with { Slug = "Bad Slug!" };

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateNewsCommand.Slug));
    }
}
