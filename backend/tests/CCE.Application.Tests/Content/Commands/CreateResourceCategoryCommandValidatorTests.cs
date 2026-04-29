using CCE.Application.Content.Commands.CreateResourceCategory;

namespace CCE.Application.Tests.Content.Commands;

public class CreateResourceCategoryCommandValidatorTests
{
    private readonly CreateResourceCategoryCommandValidator _sut = new();

    [Fact]
    public void Valid_command_passes_validation()
    {
        var cmd = new CreateResourceCategoryCommand("طاقة", "Energy", "energy", null, 0);
        var result = _sut.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_NameAr_fails_validation()
    {
        var cmd = new CreateResourceCategoryCommand("", "Energy", "energy", null, 0);
        var result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(cmd.NameAr));
    }

    [Fact]
    public void Negative_OrderIndex_fails_validation()
    {
        var cmd = new CreateResourceCategoryCommand("طاقة", "Energy", "energy", null, -1);
        var result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(cmd.OrderIndex));
    }
}
