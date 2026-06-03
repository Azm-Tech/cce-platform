using CCE.Application.Common;
using CCE.Application.Content.Dtos;
using MediatR;

namespace CCE.Application.Content.Commands.Tags.UpdateTag;

public sealed record UpdateTagCommand(
    System.Guid Id,
    string NameAr,
    string NameEn,
    string? Color) : IRequest<Response<TagDto>>;
