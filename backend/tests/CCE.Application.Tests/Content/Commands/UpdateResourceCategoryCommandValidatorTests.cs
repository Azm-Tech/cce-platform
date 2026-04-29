using CCE.Application.Content.Commands.UpdateResourceCategory;

namespace CCE.Application.Tests.Content.Commands;

public class UpdateResourceCategoryCommandValidatorTests
{
    private readonly UpdateResourceCategoryCommandValidator _sut = new();

    [Fact]
    public void Valid_command_passes_validation()
    {
        var cmd = new UpdateResourceCategoryCommand(System.Guid.NewGuid(), "طاقة", "Energy", 3, true);
        var result = _sut.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_NameEn_fails_validation()
    {
        var cmd = new UpdateResourceCategoryCommand(System.Guid.NewGuid(), "طاقة", "", 3, true);
        var result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(cmd.NameEn));
    }
}
