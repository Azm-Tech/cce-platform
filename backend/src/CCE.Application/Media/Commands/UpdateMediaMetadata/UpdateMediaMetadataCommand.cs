using CCE.Application.Common;
using CCE.Application.Media.Dtos;
using MediatR;

namespace CCE.Application.Media.Commands.UpdateMediaMetadata;

public sealed record UpdateMediaMetadataCommand(
    System.Guid Id,
    string? TitleAr,
    string? TitleEn,
    string? DescriptionAr,
    string? DescriptionEn,
    string? AltTextAr,
    string? AltTextEn) : IRequest<Response<MediaFileBriefDto>>;
