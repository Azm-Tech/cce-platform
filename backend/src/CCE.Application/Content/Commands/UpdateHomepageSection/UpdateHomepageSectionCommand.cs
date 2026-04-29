using CCE.Application.Content.Dtos;
using MediatR;

namespace CCE.Application.Content.Commands.UpdateHomepageSection;

public sealed record UpdateHomepageSectionCommand(
    System.Guid Id,
    string ContentAr,
    string ContentEn,
    bool IsActive) : IRequest<HomepageSectionDto?>;
