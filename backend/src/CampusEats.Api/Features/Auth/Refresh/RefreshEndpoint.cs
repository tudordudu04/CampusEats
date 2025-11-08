using MediatR;

namespace CampusEats.Api.Features.Auth.Refresh;

public static class RefreshEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/refresh", async (IMediator mediator, CancellationToken ct) =>
            {
                var access = await mediator.Send(new RefreshCommand(), ct);
                return Results.Ok(new { AccessToken = access });
            })
            .WithTags("Auth");
    }
}