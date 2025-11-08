using CampusEats.Api.Data;
using CampusEats.Api.Infrastructure.Auth;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Api.Features.Auth.Refresh;

public class RefreshHandler(
    AppDbContext db,
    IJwtTokenService jwt,
    IHttpContextAccessor http
) : IRequestHandler<RefreshCommand, string>
{
    public async Task<string> Handle(RefreshCommand request, CancellationToken ct)
    {
        var ctx = http.HttpContext!;
        if (!ctx.Request.Cookies.TryGetValue("refresh_token", out var token) || string.IsNullOrWhiteSpace(token))
            throw new UnauthorizedAccessException("Missing refresh token.");

        var hash = jwt.Hash(token);
        var rt = await db.RefreshTokens.FirstOrDefaultAsync(t =>
            t.TokenHash == hash && t.RevokedAtUtc == null && t.ExpiresAtUtc > DateTime.UtcNow, ct);

        if (rt is null) throw new UnauthorizedAccessException("Invalid refresh token.");

        var user = await db.Users.AsNoTracking().FirstAsync(u => u.Id == rt.UserId, ct);

        // Rotate token
        rt.RevokedAtUtc = DateTime.UtcNow;
        var (newToken, newHash, expiresAt) = jwt.GenerateRefreshToken(days: 7);
        db.RefreshTokens.Add(new Domain.RefreshToken
        {
            UserId = user.Id,
            TokenHash = newHash,
            ExpiresAtUtc = expiresAt
        });

        await db.SaveChangesAsync(ct);

        ctx.Response.Cookies.Append(
            "refresh_token",
            newToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = expiresAt
            });

        return jwt.GenerateAccessToken(user);
    }
}