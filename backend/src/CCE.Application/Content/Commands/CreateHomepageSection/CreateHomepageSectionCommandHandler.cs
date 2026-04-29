using CCE.Application.Content.Dtos;
using CCE.Application.Content.Queries.ListHomepageSections;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Commands.CreateHomepageSection;

public sealed class CreateHomepageSectionCommandHandler : IRequestHandler<CreateHomepageSectionCommand, HomepageSectionDto>
{
    private readonly IHomepageSectionService _service;

    public CreateHomepageSectionCommandHandler(IHomepageSectionService service)
    {
        _service = service;
    }

    public async Task<HomepageSectionDto> Handle(CreateHomepageSectionCommand request, CancellationToken cancellationToken)
    {
        var section = HomepageSection.Create(
            request.SectionType,
            request.OrderIndex,
            request.ContentAr,
            request.ContentEn);
        await _service.SaveAsync(section, cancellationToken).ConfigureAwait(false);
        return ListHomepageSectionsQueryHandler.MapToDto(section);
    }
}
