using CampusEats.Api.Data;
using CampusEats.Api.Infrastructure.Auth;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Api.Features.Auth.Logout;

public class LogoutHandler(
    AppDbContext db,
    IJwtTokenService jwt,
    IHttpContextAccessor http
) : IRequestHandler<LogoutCommand, bool>
{
    public async Task<bool> Handle(LogoutCommand request, CancellationToken ct)
    {
        var ctx = http.HttpContext!;
        if (!ctx.Request.Cookies.TryGetValue("refresh_token", out var token))
            return true;

        var hash = jwt.Hash(token);
        var tokens = await db.RefreshTokens.Where(t => t.TokenHash == hash && t.RevokedAtUtc == null).ToListAsync(ct);
        foreach (var t in tokens) t.RevokedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        ctx.Response.Cookies.Delete("refresh_token", new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict });
        return true;
    }
}