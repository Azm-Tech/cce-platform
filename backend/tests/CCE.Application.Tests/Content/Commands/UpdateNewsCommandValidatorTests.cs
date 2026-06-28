using CCE.Application.Content.Commands.UpdateNews;

namespace CCE.Application.Tests.Content.Commands;

public class UpdateNewsCommandValidatorTests
{
    private static UpdateNewsCommand ValidCmd() => new(
        System.Guid.NewGuid(),
        "خبر", "News", "محتوى", "Content",
        System.Guid.NewGuid(), null);

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
    public void Empty_topic_id_is_rejected()
    {
        var sut = new UpdateNewsCommandValidator();
        var cmd = ValidCmd() with { TopicId = System.Guid.Empty };

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateNewsCommand.TopicId));
    }
}
