using System.Reflection;
using CampusEats.Api.Data;
using CampusEats.Api.Features.Menu.CreateMenuItem;
using CampusEats.Api.Features.Menu.DeleteMenuItem;
using CampusEats.Api.Features.Menu.GetAllMenuItems;
using CampusEats.Api.Features.Menu.GetMenuItem;
using CampusEats.Api.Features.Menu.UpdateMenuItem;
using CampusEats.Api.Infrastructure.Validation;
using CampusEats.Api.Validation;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var connectionString = "Host=localhost;Port=5432;Database=campuseats;Username=postgres;Password=postgres";

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc
    (
        "v1",
        new OpenApiInfo
        {
            Title = "CampusEats API",
            Version = "v1",
            Description = "API for managing campus food services",
            Contact = new OpenApiContact
            {
                Name = "API Support",
                Email = "support@example.com",
            }
        });
});
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

const string corsPolicy = "frontend";
// builder.Services.AddCors(options =>
// {
//     options.AddPolicy(corsPolicy, policy =>
//     {
//         policy.WithOrigins("http://localhost:5173")
//             .AllowAnyHeader()
//             .AllowAnyMethod();
//     });
// });

var app = builder.Build();

// app.UseCors(corsPolicy);
app.UseMiddleware<ValidationExceptionMiddleware>();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); 
    app.UseSwaggerUI
    (
        c=>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "CampusEats API");
            c.RoutePrefix = string.Empty;
            c.DisplayRequestDuration();
        }
    );
    
}

app.UseHttpsRedirection();

app.MapGet("/", () => Results.Ok(new { name = "CampusEats API", status = "ok" }));

// TODO: Add your feature endpoint mappings here (Menu CRUD)
CreateMenuItemEndpoint.Map(app);
GetAllMenuItemsEndpoint.Map(app);
GetMenuItemEndpoint.Map(app);
UpdateMenuItemEndpoint.Map(app);
DeleteMenuItemEndpoint.Map(app);

app.Run();