using WorldCupBets.WebApi.Configuration;
using Xunit;

namespace WorldCupBets.Application.Tests;

public sealed class SecurityConfigurationTests
{
    [Fact]
    public void JwtOptions_Rejects_Weak_Secret()
    {
        var options = CreateJwtOptions(
            secret: "too-short",
            accessTokenLifetimeMinutes: 60);

        var exception = Assert.Throws<InvalidOperationException>(options.Validate);

        Assert.Contains("at least 32", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(121)]
    public void JwtOptions_Rejects_Unsafe_Access_Token_Lifetime(int lifetimeMinutes)
    {
        var options = CreateJwtOptions(
            secret: "0123456789abcdef0123456789abcdef",
            accessTokenLifetimeMinutes: lifetimeMinutes);

        var exception = Assert.Throws<InvalidOperationException>(options.Validate);

        Assert.Contains("between 5 and 120", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AuthOptions_Disables_Dev_Login_By_Default()
    {
        var options = new AuthOptions();

        Assert.False(options.EnableDevLogin);
    }

    private static JwtOptions CreateJwtOptions(string secret, int accessTokenLifetimeMinutes)
    {
        return new JwtOptions
        {
            Secret = secret,
            Issuer = "WorldCupBets",
            Audience = "WorldCupBets.Frontend",
            AccessTokenLifetimeMinutes = accessTokenLifetimeMinutes
        };
    }
}
