using CCE.Application.Common;
using CCE.Application.Media.Dtos;
using MediatR;

namespace CCE.Application.Media.Commands.DeleteMedia;

public sealed record DeleteMediaCommand(System.Guid Id) : IRequest<Response<MediaFileBriefDto>>;
