using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Content.Commands.FollowNews;

public sealed record FollowNewsCommand : IRequest<Response<VoidData>>;
