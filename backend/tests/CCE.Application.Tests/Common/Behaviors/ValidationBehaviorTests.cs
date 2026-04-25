using CCE.Application.Common.Behaviors;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CCE.Application.Tests.Common.Behaviors;

public sealed record ValidationBehaviorTestRequest(string Name) : IRequest<string>;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Passes_through_when_no_validators_registered()
    {
        var sut = new ValidationBehavior<ValidationBehaviorTestRequest, string>(validators: []);

        var result = await sut.Handle(new ValidationBehaviorTestRequest("ok"), () => Task.FromResult("done"), CancellationToken.None);

        result.Should().Be("done");
    }

    [Fact]
    public async Task Passes_through_when_all_validators_pass()
    {
        var validator = Substitute.For<IValidator<ValidationBehaviorTestRequest>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<ValidationBehaviorTestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var sut = new ValidationBehavior<ValidationBehaviorTestRequest, string>([validator]);

        var result = await sut.Handle(new ValidationBehaviorTestRequest("ok"), () => Task.FromResult("done"), CancellationToken.None);

        result.Should().Be("done");
    }

    [Fact]
    public async Task Throws_aggregated_ValidationException_on_failures()
    {
        var v1 = Substitute.For<IValidator<ValidationBehaviorTestRequest>>();
        v1.ValidateAsync(Arg.Any<ValidationContext<ValidationBehaviorTestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([new ValidationFailure("Name", "must not be empty")]));
        var v2 = Substitute.For<IValidator<ValidationBehaviorTestRequest>>();
        v2.ValidateAsync(Arg.Any<ValidationContext<ValidationBehaviorTestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([new ValidationFailure("Name", "must be at least 3 chars")]));

        var sut = new ValidationBehavior<ValidationBehaviorTestRequest, string>([v1, v2]);

        var act = async () => await sut.Handle(new ValidationBehaviorTestRequest(""), () => Task.FromResult("done"), CancellationToken.None);

        var ex = (await act.Should().ThrowAsync<ValidationException>()).Subject.First();
        ex.Errors.Should().HaveCount(2);
        ex.Errors.Should().Contain(f => f.ErrorMessage == "must not be empty");
        ex.Errors.Should().Contain(f => f.ErrorMessage == "must be at least 3 chars");
    }
}
