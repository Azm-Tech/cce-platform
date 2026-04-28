using CCE.Application.Content.Dtos;
using MediatR;

namespace CCE.Application.Content.Commands.PublishResource;

public sealed record PublishResourceCommand(System.Guid Id) : IRequest<ResourceDto?>;
