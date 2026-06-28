using CCE.Application.Common;
using CCE.Application.Content.Dtos;
using MediatR;

namespace CCE.Application.Content.Commands.Tags.CreateTag;

public sealed record CreateTagCommand(
    string NameAr,
    string NameEn,
    string? Color) : IRequest<Response<TagDto>>;
