using System.Security.Claims;
using CampusEats.Api.Data;
using CampusEats.Api.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Api.Features.Auth.DeleteUser;

public class DeleteUserHandler(
    AppDbContext db,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<DeleteUserCommand, IResult>
{
    public async Task<IResult> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null || !httpContext.User.Identity?.IsAuthenticated == true)
            return Results.Unauthorized();

        var roleClaim = httpContext.User.FindFirst(ClaimTypes.Role)?.Value;
        if (!Enum.TryParse<UserRole>(roleClaim, out var role) || role != UserRole.MANAGER)
            return Results.Forbid();

        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
            return Results.NotFound("User not found");

        var refreshTokens = db.RefreshTokens.Where(rt => rt.UserId == request.UserId);
        db.RefreshTokens.RemoveRange(refreshTokens);

        db.Users.Remove(user);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok();
    }
}