using AlacrityCore.Utils;
using Microsoft.AspNetCore.Identity;

namespace AlacrityTests.Tests.UtilsTests;
public class AuthenticationUtilTests
{
    [Test]
    public void HashPasswordDoesNotThrow()
    {
        var hashedPassword = AuthenticationUtil.HashPassword("ToTheMoon+1");
    }

    [Test]
    public void PasswordHashingValidatesCorrectly()
    {
        var password = "AllGoodThingsComeToAnEnd";
        var hashedPassword = AuthenticationUtil.HashPassword(password);
        var verified = AuthenticationUtil.VerifyPassword(password, hashedPassword);
        Assert.That(verified, Is.EqualTo(PasswordVerificationResult.Success));
    }

    [TestCase("AG00dPassword!")]
    [TestCase("Wh4tAPrettyPassword@")]
    [TestCase("$£AndAnotherBigOne1111")]
    public void IsPasswordComplex_ValidPasswords(string password)
        => Assert.That(AuthenticationUtil.IsPasswordComplex(password), Is.True);

    [TestCase(null)]
    [TestCase("")]
    [TestCase("a")]
    [TestCase("hello")]
    [TestCase("VeryBasic")]
    [TestCase("12InvalidSymbols____")]
    [TestCase("NeedsMoreComplexity")]
    public void IsPasswordComplex_InvalidPasswords(string? password)
        => Assert.That(AuthenticationUtil.IsPasswordComplex(password), Is.False);
    
}
