using MediatR;

namespace CampusEats.Api.Features.Auth.Logout;

public static class LogoutEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/logout", async (IMediator mediator, CancellationToken ct) =>
            {
                var ok = await mediator.Send(new LogoutCommand(), ct);
                return ok ? Results.NoContent() : Results.Ok();
            })
            .WithTags("Auth");
    }
}