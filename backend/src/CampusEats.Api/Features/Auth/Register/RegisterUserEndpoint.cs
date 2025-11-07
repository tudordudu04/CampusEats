using MediatR;

namespace CampusEats.Api.Features.Auth.Register;

public static class RegisterUserEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/register", async (RegisterUserCommand cmd, IMediator mediator, CancellationToken ct) =>
            {
                var result = await mediator.Send(cmd, ct);
                return Results.Created("/auth/register", result);
            })
            .WithTags("Auth");
    }
}