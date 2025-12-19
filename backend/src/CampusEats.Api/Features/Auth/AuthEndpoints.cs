using CampusEats.Api.Features.Auth.Login;
using CampusEats.Api.Features.Auth.Logout;
using CampusEats.Api.Features.Auth.Refresh;
using CampusEats.Api.Features.Auth.Register;
using CampusEats.Api.Features.Auth.DeleteUser;
using CampusEats.Api.Features.Auth.GetAllUsers;
using CampusEats.Api.Features.Auth.UpdateUserProfile;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using CampusEats.Api.Features.Auth.GetUser;
using CampusEats.Api.Features.Auth.UploadProfilePicture;

namespace CampusEats.Api.Features.Auth;

public static class AuthEndpoints
{
    public static void MapAuth(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", async (LoginUserCommand cmd, IMediator mediator, CancellationToken ct) =>
            {
                var result = await mediator.Send(cmd, ct);
                return result;
            })
            .WithTags("Auth");
        
        app.MapPost("/auth/logout", async (IMediator mediator, CancellationToken ct) =>
            {
                var result = await mediator.Send(new LogoutCommand(), ct);
                return result;
            })
            .WithTags("Auth");
        
        app.MapPost("/auth/refresh", async (IMediator mediator, CancellationToken ct) =>
            {
                var result = await mediator.Send(new RefreshCommand(), ct);
                return result;
            })
            .WithTags("Auth");
        
        app.MapPost("/auth/register", async (RegisterUserCommand cmd, IMediator mediator, CancellationToken ct) =>
            {
                var result = await mediator.Send(cmd, ct);
                return result;
            })
            .WithTags("Auth");
        
        app.MapDelete("/auth/delete",
                async ([FromBody] DeleteUserCommand cmd,
                    [FromServices] IMediator mediator,
                    CancellationToken ct) =>
                {
                    var result = await mediator.Send(cmd, ct);
                    return result;
                })
            .WithTags("Auth");
        app.MapGet("/auth/users", async (IMediator mediator, CancellationToken ct) =>
            {
                var result = await mediator.Send(new GetAllUsersQuery(), ct);
                return result;
            })
            .WithTags("Auth");
        app.MapPut("/auth/profile", async (UpdateUserProfileCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result;
        }).RequireAuthorization().WithTags("Auth");
        app.MapGet("/auth/me", async (ISender sender,IHttpContextAccessor httpContextAccessor)=>
        {
            var userId = httpContextAccessor.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            if (userId == null) return Results.Unauthorized();
            
            
            var result = await sender.Send(new GetUserQuery(Guid.Parse(userId)));
            
            return result != null ? Results.Ok(result) : Results.NotFound();
        }).RequireAuthorization().WithTags("Auth");

        app.MapPost("/auth/profile-picture", async (
            IFormFile file,
            ISender sender, CancellationToken cancellationToken) =>
        {
            if (file == null || file.Length == 0)
            {
                return Results.BadRequest("Te rog să încarci o poză validă.");
            }

            using var stream = file.OpenReadStream();
            var command = new UploadProfilePictureCommand(stream, file.FileName);
            var result = await sender.Send(command, cancellationToken);
            return Results.Ok(result);
        }).RequireAuthorization().DisableAntiforgery();
    }
}