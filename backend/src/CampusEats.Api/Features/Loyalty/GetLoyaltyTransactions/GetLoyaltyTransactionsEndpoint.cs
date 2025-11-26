using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CampusEats.Api.Features.Loyalty.GetLoyaltyTransactions;

public static class GetLoyaltyTransactionsEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/loyalty/transactions", async (
                [FromServices] IMediator mediator,
                ClaimsPrincipal user) =>
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                    return Results.Unauthorized();

                var transactions = await mediator.Send(new GetLoyaltyTransactionsQuery(userId));
                return Results.Ok(transactions);
            })
            // .RequireAuthorization()
            .WithTags("Loyalty")
            .WithOpenApi();
    }
}