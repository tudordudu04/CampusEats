using MediatR;

namespace CampusEats.Api.Features.Payments.CreatePaymentSession;

// Am adăugat string? Notes
public record CreatePaymentSessionCommand(List<OrderItemDto> Items, string? Notes) : IRequest<CreatePaymentSessionResult>;

public record OrderItemDto(string MenuItemId, int Quantity);

public record CreatePaymentSessionResult(string SessionId, string CheckoutUrl);