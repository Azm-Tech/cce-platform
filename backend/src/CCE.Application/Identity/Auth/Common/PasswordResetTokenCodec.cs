namespace CCE.Application.Identity.Auth.Common;

public static class PasswordResetTokenCodec
{
    public static string Encode(string token)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(token);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public static string Decode(string encodedToken)
    {
        var incoming = encodedToken.Replace('-', '+').Replace('_', '/');
        var padding = incoming.Length % 4;
        if (padding > 0)
        {
            incoming = incoming.PadRight(incoming.Length + 4 - padding, '=');
        }

        var bytes = Convert.FromBase64String(incoming);
        return System.Text.Encoding.UTF8.GetString(bytes);
    }
}
