using CampusEats.Api.Data;
using CampusEats.Api.Infrastructure.Auth;
using CampusEats.Api.Infrastructure.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Api.Features.Auth.Login;

public class LoginUserHandler(
    AppDbContext db,
    IPasswordService passwords,
    IJwtTokenService jwt,
    IHttpContextAccessor http
) : IRequestHandler<LoginUserCommand, IResult>
{
    public async Task<IResult> Handle(LoginUserCommand request, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email, ct);
        if (user is null || !passwords.Verify(user, user.PasswordHash, request.Password))
            return Results.BadRequest("Invalid email or password.");

        var (rt, rtHash, expiresAt) = jwt.GenerateRefreshToken();
        var tokens = db.RefreshTokens.Where(t => t.UserId == user.Id && t.RevokedAtUtc == null);
        await tokens.ForEachAsync(t => t.RevokedAtUtc = DateTime.UtcNow, ct);
        db.RefreshTokens.Add(new Domain.RefreshToken
        {
            UserId = user.Id,
            TokenHash = rtHash,
            ExpiresAtUtc = expiresAt
        });

        await db.SaveChangesAsync(ct);

        http.HttpContext!.Response.Cookies.Append(
            "refresh_token",
            rt,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = expiresAt
            });

        var access = jwt.GenerateAccessToken(user);
        return Results.Ok(new AuthResultDto(access));
    }
}