using CCE.Domain.Verification;

namespace CCE.Application.Identity;

public interface IUserRepository
{
    Task<Guid?> FindUserIdByContactAsync(string contact, OtpVerificationType type, CancellationToken ct = default);
    Task StampConfirmedAsync(Guid userId, OtpVerificationType type, CancellationToken ct = default);
    Task<bool> IsContactTakenAsync(string contact, OtpVerificationType type, Guid excludeUserId, CancellationToken ct = default);
}
