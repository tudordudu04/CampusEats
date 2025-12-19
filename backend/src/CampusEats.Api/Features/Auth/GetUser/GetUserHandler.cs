using CampusEats.Api.Data;
using CampusEats.Api.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace CampusEats.Api.Features.Auth.GetUser;

public class GetUserHandler : IRequestHandler<GetUserQuery,UserDto?>
{
    private readonly AppDbContext _dbContext;

    public GetUserHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserDto?> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        return user?.ToDto();
    }
    
    
}