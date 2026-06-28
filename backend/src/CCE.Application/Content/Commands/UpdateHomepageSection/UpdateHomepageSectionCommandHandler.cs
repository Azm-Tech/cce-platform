using CCE.Application.Common;
using CCE.Application.Content.Dtos;
using CCE.Application.Content.Queries.ListHomepageSections;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.Content.Commands.UpdateHomepageSection;

public sealed class UpdateHomepageSectionCommandHandler : IRequestHandler<UpdateHomepageSectionCommand, Response<HomepageSectionDto>>
{
    private readonly IHomepageSectionRepository _service;
    private readonly MessageFactory _msg;

    public UpdateHomepageSectionCommandHandler(IHomepageSectionRepository service, MessageFactory msg)
    {
        _service = service;
        _msg = msg;
    }

    public async Task<Response<HomepageSectionDto>> Handle(UpdateHomepageSectionCommand request, CancellationToken cancellationToken)
    {
        var section = await _service.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (section is null)
        {
            return _msg.NotFound<HomepageSectionDto>(MessageKeys.PlatformSettings.HOMEPAGE_SECTION_NOT_FOUND);
        }

        section.UpdateContent(request.ContentAr, request.ContentEn);

        if (request.IsActive)
            section.Activate();
        else
            section.Deactivate();

        await _service.UpdateAsync(section, cancellationToken).ConfigureAwait(false);

        return _msg.Ok(ListHomepageSectionsQueryHandler.MapToDto(section), MessageKeys.Content.CONTENT_UPDATED);
    }
}
