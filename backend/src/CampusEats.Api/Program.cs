using System.Reflection;
using System.Text;
using CampusEats.Api.Data;
using CampusEats.Api.Features.Auth.Login;
using CampusEats.Api.Features.Auth.Logout;
using CampusEats.Api.Features.Auth.Refresh;
using CampusEats.Api.Features.Auth.Register;
using CampusEats.Api.Features.Menu.CreateMenuItem;
using CampusEats.Api.Features.Menu.DeleteMenuItem;
using CampusEats.Api.Features.Menu.GetAllMenuItems;
using CampusEats.Api.Features.Menu.GetMenuItem;
using CampusEats.Api.Features.Menu.UpdateMenuItem;
using CampusEats.Api.Features.Orders;
using CampusEats.Api.Infrastructure.Auth;
using CampusEats.Api.Infrastructure.Security;
using CampusEats.Api.Infrastructure.Validation;
using CampusEats.Api.Validation;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var connectionString = "Host=localhost;Port=5432;Database=campuseats;Username=postgres;Password=postgres";


builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
var jwtOpts = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions
{
    Issuer = "CampusEats",
    Audience = "CampusEatsClient",
    SigningKey = builder.Configuration["Jwt:SigningKey"],
};
builder.Services.AddSingleton(jwtOpts);
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<PasswordHasher<CampusEats.Api.Domain.User>>();
builder.Services.AddSingleton<IPasswordService, PasswordService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtOpts.Issuer,
            ValidAudience = jwtOpts.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOpts.SigningKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();

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
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Id = "Bearer", Type = ReferenceType.SecurityScheme } },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddDbContext<AppDbContext>(options => 
    options.UseSqlite("DataSource=campuseats.db"));

builder.Services.AddHttpContextAccessor();
const string corsPolicy = "frontend";
builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicy, policy =>
    {
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors(corsPolicy);
app.UseMiddleware<ValidationExceptionMiddleware>();
app.UseHttpsRedirection();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
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

app.MapGet("/", () => Results.Ok(new { name = "CampusEats API", status = "ok" }));

CreateMenuItemEndpoint.Map(app);
GetAllMenuItemsEndpoint.Map(app);
GetMenuItemEndpoint.Map(app);
UpdateMenuItemEndpoint.Map(app);
DeleteMenuItemEndpoint.Map(app);

RegisterUserEndpoint.Map(app);
LoginUserEndpoint.Map(app);
RefreshEndpoint.Map(app);
LogoutEndpoint.Map(app);
OrdersEndpoint.MapOrders(app);
app.Run();