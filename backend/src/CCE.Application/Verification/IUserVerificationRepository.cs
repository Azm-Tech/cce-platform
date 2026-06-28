using CCE.Application.Common.Interfaces;
using CCE.Domain.Verification;

namespace CCE.Application.Verification;

public interface IUserVerificationRepository : IRepository<UserVerification, Guid>
{
    Task<UserVerification?> FindAsync(
        string contact, OtpVerificationType typeId, CancellationToken ct = default);
}
