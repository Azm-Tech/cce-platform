using CCE.Application.Common;
using MediatR;

namespace CCE.Application.PlatformSettings.Commands.DeleteFaq;

public sealed record DeleteFaqCommand(System.Guid Id) : IRequest<Response<VoidData>>;
