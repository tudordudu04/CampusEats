using System.Reflection;
using System.Text;
using CampusEats.Api.Data;
using CampusEats.Api.Features.Auth;
using CampusEats.Api.Features.Auth.Login;
using CampusEats.Api.Features.Auth.Logout;
using CampusEats.Api.Features.Auth.Refresh;
using CampusEats.Api.Features.Auth.Register;
using CampusEats.Api.Features.Inventory.AdjustStock;
using CampusEats.Api.Features.Kitchen.CreateKitchenTask;
using CampusEats.Api.Features.Kitchen.DeleteByIdKitchenTask;
using CampusEats.Api.Features.Kitchen.GetAllKitchenTasks;
using CampusEats.Api.Features.Kitchen.GetKitchenTasksByStatus;
using CampusEats.Api.Features.Inventory.CreateIngredient;
using CampusEats.Api.Features.Inventory.GetAllIngredientsInStock;
using CampusEats.Api.Features.Inventory.GetStockByName;
using CampusEats.Api.Features.Kitchen.UpdateKitchenTask;
using CampusEats.Api.Features.Menu.CreateMenuItem;
using CampusEats.Api.Features.Menu.DeleteMenuItem;
using CampusEats.Api.Features.Menu.GetAllMenuItems;
using CampusEats.Api.Features.Menu.GetMenuItem;
using CampusEats.Api.Features.Menu.UpdateMenuItem;
using CampusEats.Api.Features.Orders;
using CampusEats.Api.Features.Payments.ConfirmPayment;
using CampusEats.Api.Features.Payments.CreatePaymentSession;
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
using Stripe;
using CampusEats.Api.Features.Loyalty.GetLoyaltyAccount;
using CampusEats.Api.Features.Loyalty.GetLoyaltyTransactions;
using CampusEats.Api.Features.Loyalty.RedeemPoints;
using CampusEats.Api.Features.Menu;
using CampusEats.Api.Features.Payments;
using CampusEats.Api.Features.Coupons;

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
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];
builder.Services.AddScoped<CampusEats.Api.Infrastructure.Loyalty.ILoyaltyService, CampusEats.Api.Infrastructure.Loyalty.LoyaltyService>();
builder.WebHost.UseWebRoot("wwwroot");

var app = builder.Build();

app.UseCors(corsPolicy);
app.UseMiddleware<ValidationExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseStaticFiles(); 

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<PasswordHasher<CampusEats.Api.Domain.User>>();
    await db.Database.EnsureCreatedAsync();
    
    var managerName = builder.Configuration["SeedManager:Name"];
    var managerEmail = builder.Configuration["SeedManager:Email"];
    var managerPassword = builder.Configuration["SeedManager:Password"];
    
    await db.EnsureSeedManagerAsync(managerName, managerEmail, managerPassword, hasher);
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

MenuEndpoints.MapMenu(app);
AuthEndpoints.MapAuth(app);
OrdersEndpoint.MapOrders(app);
PaymentsEndpoints.MapPayments(app);

CreateKitchenTaskEndpoint.Map(app);
GetAllKitchenTasksEndpoint.Map(app);
DeleteKitchenTaskEndpoint.Map(app);
GetKitchenTasksByStatusEndpoint.Map(app);
UpdateKitchenTaskEndpoint.Map(app);

CreateIngredientEndpoint.Map(app);
AdjustStockEndpoint.Map(app);
GetAllIngredientsInStockEndpoint.Map(app);
GetStockByNameEndpoint.Map(app);

GetLoyaltyAccountEndpoint.Map(app);
GetLoyaltyTransactionsEndpoint.Map(app);
RedeemPointsEndpoint.Map(app);

CouponEndpoints.MapCouponEndpoints(app);

app.Run();