
using MediatR;

namespace CampusEats.Api.Features.Inventory.GetStockByName;

public record GetStockByNameCommand(string Name) : IRequest<IResult>;