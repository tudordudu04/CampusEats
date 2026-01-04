using CampusEats.Api.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace CampusEats.Tests;

public class IntegrationTestBase : IClassFixture<WebApplicationFactory<Program>>
{
    protected readonly HttpClient Client;
    protected readonly IServiceProvider Services;

    public IntegrationTestBase(WebApplicationFactory<Program> factory)
    {
        var testFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Suprascriem string-ul de conexiune pentru teste
                // Folosim un fișier unic pentru fiecare rulare pentru a evita conflictele
                var testDbName = $"test_db_{Guid.NewGuid()}.db";
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = $"DataSource={testDbName}"
                });
            });

            builder.ConfigureServices(services =>
            {
                // Nu mai este nevoie să eliminăm manual DbContext-ul 
                // deoarece folosim sistemul de configurare pentru a schimba baza de date
            });
        });

        Client = testFactory.CreateClient();
        Services = testFactory.Services;
    }
}