using CCE.Application.Common.Interfaces;
using CCE.Domain.Verification;

namespace CCE.Application.Verification;

public interface IOtpVerificationRepository : IRepository<OtpVerification, Guid>
{
    Task<OtpVerification?> FindActiveAsync(
        string contact, OtpVerificationType typeId,
        DateTimeOffset now, CancellationToken ct = default);
}
