using MediatR;

namespace CampusEats.Api.Features.Auth.Login;

public static class LoginUserEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", async (LoginUserCommand cmd, IMediator mediator, CancellationToken ct) =>
            {
                var result = await mediator.Send(cmd, ct);
                return Results.Ok(result);
            })
            .WithTags("Auth");
    }
}