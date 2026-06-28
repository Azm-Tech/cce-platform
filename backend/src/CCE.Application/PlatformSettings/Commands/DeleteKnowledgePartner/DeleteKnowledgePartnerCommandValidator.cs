using FluentValidation;

namespace CCE.Application.PlatformSettings.Commands.DeleteKnowledgePartner;

public sealed class DeleteKnowledgePartnerCommandValidator
    : AbstractValidator<DeleteKnowledgePartnerCommand>
{
    public DeleteKnowledgePartnerCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
