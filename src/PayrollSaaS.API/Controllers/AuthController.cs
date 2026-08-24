using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PayrollSaaS.Domain.Entities.Auth;
using PayrollSaaS.Infrastructure.Persistence;

namespace PayrollSaaS.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly PayrollDbContext _db;
    private readonly IConfiguration _config;

    public AuthController(PayrollDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public record LoginRequest(string Email, string Password);
    public record TokenResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);
    public record RefreshRequest(string RefreshToken);

    /// <summary>POST /auth/login — Returns access_token + refresh_token (doc §6).</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new ProblemDetails { Title = "Invalid credentials", Status = 401 });

        var (accessToken, expiresAt) = GenerateAccessToken(user);
        var refreshTokenValue = GenerateRefreshTokenValue();

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = HashToken(refreshTokenValue),
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync();

        return Ok(new TokenResponse(accessToken, refreshTokenValue, expiresAt));
    }

    /// <summary>POST /auth/refresh — Issues new access_token using refresh_token (doc §6).</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenResponse>> Refresh([FromBody] RefreshRequest request)
    {
        var hash = HashToken(request.RefreshToken);
        var existing = await _db.RefreshTokens.IgnoreQueryFilters()
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash);

        if (existing is null)
            return Unauthorized(new ProblemDetails { Title = "Invalid refresh token", Status = 401 });

        // Reuse detection: if the token was already revoked, revoke the entire chain
        if (existing.IsRevoked)
        {
            await RevokeDescendants(existing.Id);
            await _db.SaveChangesAsync();
            return Unauthorized(new ProblemDetails { Title = "Token reuse detected — all tokens revoked", Status = 401 });
        }

        if (existing.IsExpired)
            return Unauthorized(new ProblemDetails { Title = "Refresh token expired", Status = 401 });

        // Rotate: revoke old, issue new
        existing.RevokedAt = DateTime.UtcNow;

        var newRefreshValue = GenerateRefreshTokenValue();
        var newRefreshToken = new RefreshToken
        {
            UserId = existing.UserId,
            TokenHash = HashToken(newRefreshValue),
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        existing.ReplacedByTokenId = newRefreshToken.Id;

        _db.RefreshTokens.Add(newRefreshToken);
        await _db.SaveChangesAsync();

        var (accessToken, expiresAt) = GenerateAccessToken(existing.User);
        return Ok(new TokenResponse(accessToken, newRefreshValue, expiresAt));
    }

    /// <summary>POST /auth/logout — Revokes the refresh token (doc §6).</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest request)
    {
        var hash = HashToken(request.RefreshToken);
        var token = await _db.RefreshTokens.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TokenHash == hash);

        if (token is not null && !token.IsRevoked)
        {
            token.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return NoContent();
    }

    // ── Private helpers ──

    private (string token, DateTime expiresAt) GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _config["Jwt:Key"] ?? "DevKey-Change-In-Production-Must-Be-32-Bytes!!"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
        };

        if (user.SchoolId is not null)
            claims.Add(new Claim("school_id", user.SchoolId.Value.ToString()));
        if (user.EmployeeId is not null)
            claims.Add(new Claim("employee_id", user.EmployeeId.Value.ToString()));

        var expiresAt = DateTime.UtcNow.AddMinutes(30);
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "PayrollSaaS",
            audience: _config["Jwt:Audience"] ?? "PayrollSaaS",
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    private static string GenerateRefreshTokenValue() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexStringLower(bytes);
    }

    private async Task RevokeDescendants(Guid tokenId)
    {
        var token = await _db.RefreshTokens.FindAsync(tokenId);
        if (token is null) return;

        var descendants = await _db.RefreshTokens.IgnoreQueryFilters()
            .Where(t => t.UserId == token.UserId && !t.IsRevoked)
            .ToListAsync();

        foreach (var d in descendants)
            d.RevokedAt = DateTime.UtcNow;
    }
}
