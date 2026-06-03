using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Content.Commands.Tags.DeleteTag;

public sealed record DeleteTagCommand(System.Guid Id) : IRequest<Response<CCE.Application.Common.VoidData>>;
