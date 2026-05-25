using CCE.Application.Content.Dtos;
using CCE.Application.Content.Queries.ListHomepageSections;
using MediatR;

namespace CCE.Application.Content.Commands.UpdateHomepageSection;

public sealed class UpdateHomepageSectionCommandHandler : IRequestHandler<UpdateHomepageSectionCommand, HomepageSectionDto?>
{
    private readonly IHomepageSectionRepository _service;

    public UpdateHomepageSectionCommandHandler(IHomepageSectionRepository service)
    {
        _service = service;
    }

    public async Task<HomepageSectionDto?> Handle(UpdateHomepageSectionCommand request, CancellationToken cancellationToken)
    {
        var section = await _service.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (section is null)
        {
            return null;
        }

        section.UpdateContent(request.ContentAr, request.ContentEn);

        if (request.IsActive)
            section.Activate();
        else
            section.Deactivate();

        await _service.UpdateAsync(section, cancellationToken).ConfigureAwait(false);

        return ListHomepageSectionsQueryHandler.MapToDto(section);
    }
}
