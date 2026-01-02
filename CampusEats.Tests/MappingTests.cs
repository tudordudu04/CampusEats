using CampusEats.Api.Domain;
using CampusEats.Api.Enums;
using CampusEats.Api.Mappings;
using Xunit;

namespace CampusEats.Tests;

public class MappingTests
{
    [Fact]
    public void User_ToDto_Should_Map_All_Fields()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Mapping Test",
            Email = "map@test.com",
            Role = UserRole.MANAGER,
            ProfilePictureUrl = "http://photo.jpg",
            AddressCity = "Bucuresti"
        };

        var dto = user.ToDto();

        Assert.Equal(user.Name, dto.Name);
        Assert.Equal(user.Email, dto.Email);
        Assert.Equal("MANAGER", dto.Role);
        Assert.Equal(user.ProfilePictureUrl, dto.ProfilePictureUrl);
        Assert.Equal(user.AddressCity, dto.AddressCity);
    }
}