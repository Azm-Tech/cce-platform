using CCE.Application.Common;
using CCE.Application.Media.Dtos;
using MediatR;

namespace CCE.Application.Media.Commands.UploadMedia;

public sealed record UploadMediaCommand(
    Stream FileStream,
    string FileName,
    string ContentType,
    long FileSize,
    string? TitleAr,
    string? TitleEn,
    string? DescriptionAr,
    string? DescriptionEn,
    string? AltTextAr,
    string? AltTextEn) : IRequest<Response<MediaFileBriefDto>>;
