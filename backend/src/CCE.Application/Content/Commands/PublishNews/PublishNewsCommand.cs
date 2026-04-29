using CCE.Application.Content.Dtos;
using MediatR;

namespace CCE.Application.Content.Commands.PublishNews;

public sealed record PublishNewsCommand(System.Guid Id) : IRequest<NewsDto?>;
