using System.Security.Claims;
using CampusEats.Api.Data;
using CampusEats.Api.Domain;
using CampusEats.Api.Enums;
using CampusEats.Api.Infrastructure.Auth;
using CampusEats.Api.Infrastructure.Security;
using MediatR;

namespace CampusEats.Api.Features.Auth.Register;

public class RegisterUserHandler(
    AppDbContext db,
    IPasswordService passwords,
    IJwtTokenService jwt,
    IHttpContextAccessor http
) : IRequestHandler<RegisterUserCommand, IResult>
{
    public async Task<IResult> Handle(RegisterUserCommand request, CancellationToken ct)
    {
        if (request.Role != UserRole.STUDENT)
        {
            var ctx = http.HttpContext;
            if (ctx is null || ctx.User.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();

            var roleClaim = ctx.User.FindFirst(ClaimTypes.Role)?.Value;

            if (!Enum.TryParse<UserRole>(roleClaim, ignoreCase: true, out var callerRole) ||
                callerRole != UserRole.MANAGER)
            {
                return Results.Forbid();
            }
        }

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            Role = request.Role
        };
        user.PasswordHash = passwords.Hash(user, request.Password);

        db.Users.Add(user);

        var loyaltyAccount = new LoyaltyAccount
        {
            UserId = user.Id,
            Points = 0
        };

        db.LoyaltyAccounts.Add(loyaltyAccount);

        if (request.Role == UserRole.STUDENT)
        {
            var (rt, rtHash, expiresAt) = jwt.GenerateRefreshToken();
            db.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                TokenHash = rtHash,
                ExpiresAtUtc = expiresAt
            });

            await db.SaveChangesAsync(ct);

            var ctxResponse = http.HttpContext!;
            ctxResponse.Response.Cookies.Append(
                "refresh_token",
                rt,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = expiresAt
                });
        }

        var access = jwt.GenerateAccessToken(user);
        return Results.Ok(new AuthResultDto(access));
    }
}