using System.Security.Claims;
using CampusEats.Api.Data;
using CampusEats.Api.Enums;
using CampusEats.Api.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Api.Features.Auth.GetAllUsers;

public class GetAllUsersHandler(
    AppDbContext db,
    IHttpContextAccessor http
) : IRequestHandler<GetAllUsersQuery, IResult>
{
    public async Task<IResult> Handle(GetAllUsersQuery request, CancellationToken ct)
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

        var users = await db.Users
            .OrderBy(u => u.CreatedAtUtc)
            .Select(u => u.ToDto())
            .ToListAsync(ct);

        return Results.Ok(users);
    }
}