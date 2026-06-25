using CCE.Application.Common;
using CCE.Application.Content.Dtos;
using CCE.Application.Content.Queries.ListHomepageSections;
using CCE.Application.Messages;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Commands.CreateHomepageSection;

public sealed class CreateHomepageSectionCommandHandler : IRequestHandler<CreateHomepageSectionCommand, Response<HomepageSectionDto>>
{
    private readonly IHomepageSectionRepository _service;
    private readonly MessageFactory _msg;

    public CreateHomepageSectionCommandHandler(IHomepageSectionRepository service, MessageFactory msg)
    {
        _service = service;
        _msg = msg;
    }

    public async Task<Response<HomepageSectionDto>> Handle(CreateHomepageSectionCommand request, CancellationToken cancellationToken)
    {
        var section = HomepageSection.Create(
            request.SectionType,
            request.OrderIndex,
            request.ContentAr,
            request.ContentEn);
        await _service.SaveAsync(section, cancellationToken).ConfigureAwait(false);
        return _msg.Ok(ListHomepageSectionsQueryHandler.MapToDto(section), MessageKeys.Content.CONTENT_CREATED);
    }
}
