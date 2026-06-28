using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Identity.Public.Commands.ConfirmEmailChange;

public sealed record ConfirmEmailChangeCommand(
    System.Guid UserId,
    System.Guid VerificationId,
    string Code) : IRequest<Response<VoidData>>;
