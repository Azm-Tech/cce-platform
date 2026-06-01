using CCE.Application.Common;
using CCE.Application.Content.Dtos;
using MediatR;

namespace CCE.Application.Content.Commands.UpdateEvent;

public sealed record UpdateEventCommand(
    System.Guid Id,
    string TitleAr, string TitleEn,
    string DescriptionAr, string DescriptionEn,
    string? LocationAr,
    string? LocationEn,
    string? OnlineMeetingUrl,
    string? FeaturedImageUrl,
    System.Guid TopicId) : IRequest<Response<EventDto>>;
