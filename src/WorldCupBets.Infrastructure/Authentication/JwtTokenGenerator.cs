using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using WorldCupBets.Application.Abstractions;
using WorldCupBets.Application.Features.Auth;

namespace WorldCupBets.Infrastructure.Authentication;

public sealed class JwtTokenGenerator(IConfiguration configuration) : IJwtTokenGenerator
{
    public string GenerateAccessToken(AuthTokenContext context)
    {
        var now = DateTime.UtcNow;
        var options = GetOptions();
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, context.UserId.ToString()),
            new(ClaimTypes.NameIdentifier, context.UserId.ToString()),
            new(JwtRegisteredClaimNames.Email, context.Email),
            new(JwtRegisteredClaimNames.Name, context.DisplayName)
        };

        claims.AddRange(context.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = now.AddMinutes(GetAccessTokenLifetimeMinutes(options)),
            IssuedAt = now,
            Issuer = options.Issuer,
            Audience = options.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetSecret(options))),
                SecurityAlgorithms.HmacSha256)
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(tokenDescriptor);
        return handler.WriteToken(token);
    }

    private static string GetSecret(JwtAuthOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Secret))
        {
            throw new InvalidOperationException("Jwt:Secret must be configured.");
        }

        if (Encoding.UTF8.GetByteCount(options.Secret) < 32)
        {
            throw new InvalidOperationException("Jwt:Secret must be at least 32 UTF-8 bytes for HS256 signing.");
        }

        return options.Secret;
    }

    private static int GetAccessTokenLifetimeMinutes(JwtAuthOptions options)
    {
        if (options.AccessTokenLifetimeMinutes is < 5 or > 120)
        {
            throw new InvalidOperationException("Jwt:AccessTokenLifetimeMinutes must be between 5 and 120 minutes.");
        }

        return options.AccessTokenLifetimeMinutes;
    }

    private JwtAuthOptions GetOptions()
    {
        return new JwtAuthOptions
        {
            Secret = configuration["Jwt:Secret"] ?? string.Empty,
            Issuer = configuration["Jwt:Issuer"] ?? "WorldCupBets",
            Audience = configuration["Jwt:Audience"] ?? "WorldCupBets.Frontend",
            AccessTokenLifetimeMinutes = int.TryParse(configuration["Jwt:AccessTokenLifetimeMinutes"], out var minutes) ? minutes : 60
        };
    }
}
