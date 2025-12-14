using Microsoft.AspNetCore.Identity;

public static class PasswordHelper
{
    private static readonly PasswordHasher<string> _hasher = new();

    public static string Hash(string password)
    {
        return _hasher.HashPassword(null!, password);
    }

    public static bool Verify(string hash, string password)
    {
        return _hasher.VerifyHashedPassword(null!, hash, password)
            == PasswordVerificationResult.Success;
    }
}