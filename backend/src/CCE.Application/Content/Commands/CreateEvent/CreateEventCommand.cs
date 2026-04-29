using CCE.Application.Content.Dtos;
using MediatR;

namespace CCE.Application.Content.Commands.CreateEvent;

public sealed record CreateEventCommand(
    string TitleAr, string TitleEn,
    string DescriptionAr, string DescriptionEn,
    System.DateTimeOffset StartsOn,
    System.DateTimeOffset EndsOn,
    string? LocationAr,
    string? LocationEn,
    string? OnlineMeetingUrl,
    string? FeaturedImageUrl) : IRequest<EventDto>;
