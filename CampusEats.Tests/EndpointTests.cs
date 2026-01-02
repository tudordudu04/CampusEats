using System.Net;
using System.Net.Http.Json;
using CampusEats.Api.Features.Auth.Login;
using CampusEats.Api.Features.Menu;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CampusEats.Tests;

public class EndpointTests : IntegrationTestBase
{
    public EndpointTests(WebApplicationFactory<Program> factory) : base(factory) { }

    [Fact]
    public async Task GetMenu_Should_Cover_MenuEndpoints()
    {
        // Acest apel execută codul din MenuEndpoints.cs
        var response = await Client.GetAsync("/api/menu");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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