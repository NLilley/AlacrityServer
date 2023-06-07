using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace AlacrityCore.Utils;
public static class AuthenticationUtil
{
    private class DummyUser { }
    private static readonly IOptions<PasswordHasherOptions> _hasherOptions = new OptionsWrapper<PasswordHasherOptions>(new PasswordHasherOptions
    {
        IterationCount = 400_000,
    });

    public static string HashPassword(string password)
        => GetPasswordHasher().HashPassword(null, password);

    public static PasswordVerificationResult VerifyPassword(string password, string hashedPassword)
        => GetPasswordHasher().VerifyHashedPassword(null, hashedPassword, password);

    private static PasswordHasher<DummyUser> GetPasswordHasher() => new(_hasherOptions);

    private static Regex _passwordComplexity = new("^(?=.*[0-9])(?=.*[!@#$%^&*+-])[a-zA-Z0-9!@#£$%^&*+-]{8,25}$");
    public static bool IsPasswordComplex(string password) => password != null && _passwordComplexity.IsMatch(password);
}
