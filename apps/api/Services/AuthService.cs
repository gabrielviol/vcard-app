using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Api.Data.Entities;
using Microsoft.IdentityModel.Tokens;

namespace Api.Services;

public class AuthService(IConfiguration configuration)
{
    public string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password);

    public bool VerifyPassword(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);

    public string IssueToken(User user)
    {
        var jwtSecret = configuration["JWT_SECRET"]
            ?? throw new InvalidOperationException("JWT_SECRET not set");
        var issuer = configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Jwt__Issuer not set");
        var audience = configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("Jwt__Audience not set");
        var expiresMinutes = int.TryParse(configuration["Jwt:ExpiresMinutes"], out var minutes) ? minutes : 20;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
