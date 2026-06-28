namespace CCE.Application.Verification;

public interface IOtpCodeGenerator
{
    (string PlainCode, string Hash) Generate();

    bool Verify(string plainCode, string storedHash);
}
