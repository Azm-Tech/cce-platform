using CCE.Application.Common;
using CCE.Application.Content.Dtos;
using MediatR;

namespace CCE.Application.Content.Queries.DownloadFile;

public sealed record DownloadFileQuery(
    System.Guid Id,
    DownloadFileType Type = DownloadFileType.Asset) : IRequest<Response<DownloadFilePayload>>;
