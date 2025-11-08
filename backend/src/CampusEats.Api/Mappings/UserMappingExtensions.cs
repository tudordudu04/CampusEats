using CampusEats.Api.Domain;
using CampusEats.Api.Features.Auth;

namespace CampusEats.Api.Mappings;

public static class UserMappingExtensions
{
    public static UserDto ToDto(this User u) =>
        new(u.Id, u.Name, u.Email, u.Role.ToString(), u.CreatedAtUtc, u.UpdatedAtUtc);
}