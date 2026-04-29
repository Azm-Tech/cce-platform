using FluentValidation;

namespace CCE.Application.Content.Commands.RescheduleEvent;

public sealed class RescheduleEventCommandValidator : AbstractValidator<RescheduleEventCommand>
{
    public RescheduleEventCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.EndsOn).GreaterThan(x => x.StartsOn);
        RuleFor(x => x.RowVersion).NotNull().Must(rv => rv.Length == 8);
    }
}
