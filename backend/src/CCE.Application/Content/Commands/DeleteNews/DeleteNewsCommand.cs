using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Content.Commands.DeleteNews;

public sealed record DeleteNewsCommand(System.Guid Id) : IRequest<Response<VoidData>>;
