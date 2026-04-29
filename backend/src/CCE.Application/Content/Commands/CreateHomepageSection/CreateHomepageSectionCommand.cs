using CCE.Application.Content.Dtos;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Commands.CreateHomepageSection;

public sealed record CreateHomepageSectionCommand(
    HomepageSectionType SectionType,
    int OrderIndex,
    string ContentAr,
    string ContentEn) : IRequest<HomepageSectionDto>;
