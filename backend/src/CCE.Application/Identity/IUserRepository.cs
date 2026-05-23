using CCE.Domain.Verification;

namespace CCE.Application.Identity;

public interface IUserRepository
{
    Task<Guid?> FindUserIdByContactAsync(string contact, OtpVerificationType type, CancellationToken ct = default);
    void StampConfirmed(Guid userId, OtpVerificationType type);
}
