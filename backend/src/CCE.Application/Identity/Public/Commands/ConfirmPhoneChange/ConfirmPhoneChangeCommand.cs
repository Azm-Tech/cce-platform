using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Identity.Public.Commands.ConfirmPhoneChange;

public sealed record ConfirmPhoneChangeCommand(
    System.Guid UserId,
    System.Guid VerificationId,
    string Code) : IRequest<Response<VoidData>>;
