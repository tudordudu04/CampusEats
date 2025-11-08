using CampusEats.Api.Data;
using CampusEats.Api.Domain;
using CampusEats.Api.Infrastructure.Auth;
using CampusEats.Api.Infrastructure.Security;
using MediatR;

namespace CampusEats.Api.Features.Auth.Register;

public class RegisterUserHandler(
    AppDbContext db,
    IPasswordService passwords,
    IJwtTokenService jwt,
    IHttpContextAccessor http
) : IRequestHandler<RegisterUserCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(RegisterUserCommand request, CancellationToken ct)
    {
        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            Role = request.Role
        };
        
        user.PasswordHash = passwords.Hash(user, request.Password);

        db.Users.Add(user);

        var (rt, rtHash, expiresAt) = jwt.GenerateRefreshToken(days: 7);
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = rtHash,
            ExpiresAtUtc = expiresAt
        });

        await db.SaveChangesAsync(ct);

        // Set HttpOnly+Secure cookie for refresh token
        var ctx = http.HttpContext!;
        ctx.Response.Cookies.Append(
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
        return new AuthResultDto(access);
    }
}