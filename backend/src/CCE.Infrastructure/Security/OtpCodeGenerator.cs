using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CCE.Application.Verification;
using Microsoft.Extensions.Configuration;

namespace CCE.Infrastructure.Security;

public sealed class OtpCodeGenerator : IOtpCodeGenerator
{
    private readonly byte[] _secret;

    public OtpCodeGenerator(IConfiguration config)
        => _secret = Convert.FromBase64String(config["Otp:HmacSecret"]!);

    public (string PlainCode, string Hash) Generate()
    {
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
        return (code, ComputeHash(code));
    }

    public bool Verify(string plainCode, string storedHash)
        => CryptographicOperations.FixedTimeEquals(
               Encoding.UTF8.GetBytes(ComputeHash(plainCode)),
               Encoding.UTF8.GetBytes(storedHash));

    private string ComputeHash(string code)
    {
        using var hmac = new HMACSHA256(_secret);
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(code)));
    }
}
