using MediatR;

namespace CampusEats.Api.Features.Menu.DeleteMenuItem;

public record DeleteMenuItemCommand(Guid Id) : IRequest<bool>;