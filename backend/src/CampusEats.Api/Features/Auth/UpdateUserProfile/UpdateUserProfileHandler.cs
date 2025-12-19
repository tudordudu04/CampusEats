

using System.Security.Claims;
using CampusEats.Api.Data;
using MediatR;

namespace CampusEats.Api.Features.Auth.UpdateUserProfile;

public class UpdateUserProfileHandler : IRequestHandler<UpdateUserProfileCommand,IResult>
{
    public readonly AppDbContext _context;
    public readonly IHttpContextAccessor _httpContextAccessor;
    public UpdateUserProfileHandler(AppDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IResult> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
    {
        var userIdString = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if(string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Results.Unauthorized();
        }
        var user = await _context.Users.FindAsync(new object?[] { userId }, cancellationToken);
        if(user == null)
            return Results.NotFound();
        
        if(request.Name is not null)
            user.Name = request.Name;
        if(request.ProfilePictureUrl is not null)
            user.ProfilePictureUrl = request.ProfilePictureUrl;
        if(request.AddressCity is not null)
            user.AddressCity = request.AddressCity;
        if(request.AddressStreet is not null)
            user.AddressStreet = request.AddressStreet;
        if(request.AddressNumber is not null)
            user.AddressNumber = request.AddressNumber;
        if(request.AddressDetails is not null)
            user.AddressDetails = request.AddressDetails;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await  _context.SaveChangesAsync(cancellationToken);
        
        var response = new {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role.ToString(),
            ProfilePictureUrl = user.ProfilePictureUrl,
            AddressCity = user.AddressCity,
            AddressStreet = user.AddressStreet,
            AddressNumber = user.AddressNumber,
            AddressDetails = user.AddressDetails,
            CreatedAtUtc = user.CreatedAtUtc,
            UpdatedAtUtc = user.UpdatedAtUtc
        };
        return Results.Ok(response);
    }
    
}