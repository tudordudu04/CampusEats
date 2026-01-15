using System.Net;
using System.Net.Http.Json;
using CampusEats.Api.Features.Auth.Login;
using CampusEats.Api.Features.Menu;
using CampusEats.Api.Features.Menu.CreateMenuItem;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CampusEats.Tests;

public class EndpointTests(WebApplicationFactory<Program> factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetMenu_Should_Cover_MenuEndpoints()
    {
        // Acest apel execută codul din MenuEndpoints.cs
        var response = await Client.GetAsync("/api/menu");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetMenuById_Should_Return_NotFound_For_NonExistent_Item()
    {
        var nonExistentId = Guid.NewGuid();
        var response = await Client.GetAsync($"/api/menu/{nonExistentId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostMenu_Should_Create_And_Return_Created()
    {
        var command = new CreateMenuItemCommand(
            "Test Item",
            19.99m,
            "Test description",
            Api.Enums.MenuCategory.BURGER,
            null,
            new string[] { }
        );
        
        var response = await Client.PostAsJsonAsync("/api/menu", command);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var location = response.Headers.Location?.ToString();
        Assert.NotNull(location);
        Assert.Contains("/api/menu/", location);
    }

    [Fact]
    public async Task Login_With_Invalid_Data_Should_Cover_Validation_Middleware()
    {
        // Trimitem date invalide pentru a forța executarea ValidationExceptionMiddleware.cs
        var invalidLogin = new LoginUserCommand("", ""); // Câmpuri goale
        
        var response = await Client.PostAsJsonAsync("/auth/login", invalidLogin);
        
        // Ar trebui să primim 400 Bad Request prin ValidationBehaviour
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetOrders_Without_Token_Should_Cover_Auth_Challenge()
    {
        // Verificăm securitatea rutei în OrdersEndpoint.cs
        var response = await Client.GetAsync("/api/orders");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}