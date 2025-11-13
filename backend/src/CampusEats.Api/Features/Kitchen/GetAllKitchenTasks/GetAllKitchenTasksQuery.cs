using CampusEats.Api.Domain;
using CampusEats.Api.Features.Menu;
using MediatR;

namespace CampusEats.Api.Features.Kitchen.GetAllKitchenTasks;

public record GetAllKitchenTasksQuery() : IRequest<IResult>;