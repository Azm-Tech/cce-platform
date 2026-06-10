using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Content.Commands.UnfollowNews;

public sealed record UnfollowNewsCommand : IRequest<Response<VoidData>>;
