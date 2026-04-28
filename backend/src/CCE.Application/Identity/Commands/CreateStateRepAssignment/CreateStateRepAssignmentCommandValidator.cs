using FluentValidation;

namespace CCE.Application.Identity.Commands.CreateStateRepAssignment;

public sealed class CreateStateRepAssignmentCommandValidator : AbstractValidator<CreateStateRepAssignmentCommand>
{
    public CreateStateRepAssignmentCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.CountryId).NotEmpty();
    }
}
