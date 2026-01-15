using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CampusEats.Api.Data;
using CampusEats.Api.Domain;
using CampusEats.Api.Enums;
using CampusEats.Api.Features.Auth.UploadProfilePicture;
using CampusEats.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;

namespace CampusEats.Tests.AuthTests;

// Manual stub for IWebHostEnvironment to isolate file system operations during tests
class FakeWebHostEnvironment : IWebHostEnvironment
{
    public FakeWebHostEnvironment()
    {
        // Create a unique temp folder for this test run
        WebRootPath = Path.Combine(Path.GetTempPath(), "CampusEats_Test_" + Guid.NewGuid());
        ContentRootPath = WebRootPath; // Set content root to same temp folder for safety
        Directory.CreateDirectory(WebRootPath);

        // FIX: Default implementation of StaticFiles middleware requires a valid FileProvider
        WebRootFileProvider = new PhysicalFileProvider(WebRootPath);
        ContentRootFileProvider = new PhysicalFileProvider(ContentRootPath);
    }

    public string WebRootPath { get; set; }
    public IFileProvider WebRootFileProvider { get; set; }
    public string ApplicationName { get; set; } = "CampusEats.Tests";
    public IFileProvider ContentRootFileProvider { get; set; }
    public string ContentRootPath { get; set; }
    public string EnvironmentName { get; set; } = "Test";

    public void Cleanup()
    {
        // Dispose providers if they implement IDisposable (PhysicalFileProvider does)
        (WebRootFileProvider as IDisposable)?.Dispose();
        (ContentRootFileProvider as IDisposable)?.Dispose();

        if (Directory.Exists(WebRootPath))
        {
            try
            {
                Directory.Delete(WebRootPath, true);
            }
            catch
            {
                // Ignore cleanup errors (file locks, etc.)
            }
        }
    }
}

public class UploadProfilePictureTests : IDisposable
{
    private readonly FakeWebHostEnvironment _env;

    public UploadProfilePictureTests()
    {
        _env = new FakeWebHostEnvironment();
    }

    public void Dispose()
    {
        _env.Cleanup();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Handle_Should_Save_File_And_Return_Url()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("api.test.com");
        var http = new HttpContextAccessor { HttpContext = context };

        var handler = new UploadProfilePictureHandler(_env, http);

        var content = "fake image content";
        var fileName = "avatar.png";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));

        var command = new UploadProfilePictureCommand(stream, fileName);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.ProfilePictureUrl);
        Assert.StartsWith("https://api.test.com/profile-images/", result.ProfilePictureUrl);
        Assert.EndsWith(".png", result.ProfilePictureUrl);

        var savedFileName = Path.GetFileName(result.ProfilePictureUrl);
        var filePath = Path.Combine(_env.WebRootPath, "profile-images", savedFileName);

        Assert.True(File.Exists(filePath), $"File should exist at {filePath}");
        var savedContent = await File.ReadAllTextAsync(filePath);
        Assert.Equal(content, savedContent);
    }

    [Fact]
    public async Task Handle_Should_Default_To_Jpg_If_Extension_Missing()
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("localhost");
        var http = new HttpContextAccessor { HttpContext = context };

        var handler = new UploadProfilePictureHandler(_env, http);

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("content"));
        var command = new UploadProfilePictureCommand(stream, "blob"); 

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.EndsWith(".jpg", result.ProfilePictureUrl);
        
        // Verify file was actually created with .jpg extension
        var fileName = Path.GetFileName(result.ProfilePictureUrl);
        Assert.Contains(".jpg", fileName);
        var filePath = Path.Combine(_env.WebRootPath, "profile-images", fileName);
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public async Task Handle_Should_Use_Original_Extension_When_Present()
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("test.local");
        var http = new HttpContextAccessor { HttpContext = context };

        var handler = new UploadProfilePictureHandler(_env, http);

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("image data"));
        var command = new UploadProfilePictureCommand(stream, "profile.png");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.EndsWith(".png", result.ProfilePictureUrl);
        
        var fileName = Path.GetFileName(result.ProfilePictureUrl);
        var filePath = Path.Combine(_env.WebRootPath, "profile-images", fileName);
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public async Task Handle_Should_Default_To_Jpg_For_Filename_Without_Extension()
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("example.com");
        var http = new HttpContextAccessor { HttpContext = context };

        var handler = new UploadProfilePictureHandler(_env, http);

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("test"));
        
        // Filename without any extension
        var command = new UploadProfilePictureCommand(stream, "filenoext");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.EndsWith(".jpg", result.ProfilePictureUrl);
    }
}

public class UploadProfilePictureEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly FakeWebHostEnvironment _testEnv;

    public UploadProfilePictureEndpointTests(WebApplicationFactory<Program> factory)
    {
        _testEnv = new FakeWebHostEnvironment();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Clean removal of existing DB Context options
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();

                services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("TestDb_UploadPic"));

                // Override IWebHostEnvironment
                services.RemoveAll<IWebHostEnvironment>();
                services.AddSingleton<IWebHostEnvironment>(_testEnv);
            });
        });
    }

    ~UploadProfilePictureEndpointTests()
    {
        _testEnv.Cleanup();
    }

    private static AppDbContext CreateDbContext(IServiceScope scope)
        => scope.ServiceProvider.GetRequiredService<AppDbContext>();

    [Fact]
    public async Task Upload_Picture_Should_Return_Ok_And_Url()
    {
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var db = CreateDbContext(scope);
        var jwtService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        // 1. Authenticate
        var user = new User { Id = Guid.NewGuid(), Name = "Uploader", Email = "up_pic@test.com", Role=UserRole.STUDENT, PasswordHash="x" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var token = jwtService.GenerateAccessToken(user);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 2. Prepare Multipart content
        using var multipart = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent([1, 2, 3, 4]);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
        multipart.Add(fileContent, "file", "test-image.png");

        // 3. Act
        var response = await client.PostAsync("/auth/profile-picture", multipart);

        // 4. Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<UploadProfilePictureResult>();
        Assert.NotNull(result);
        Assert.NotNull(result.ProfilePictureUrl);
        Assert.Contains("profile-images", result.ProfilePictureUrl);
    }

    [Fact]
    public async Task Upload_Without_File_Should_Return_BadRequest()
    {
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var db = CreateDbContext(scope);
        var jwtService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        var user = new User { Id = Guid.NewGuid(), Name = "Uploader", Email = "up_pic_bad@test.com", Role=UserRole.STUDENT, PasswordHash="x" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var token = jwtService.GenerateAccessToken(user);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - Empty multipart
        using var multipart = new MultipartFormDataContent();
        var response = await client.PostAsync("/auth/profile-picture", multipart);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_Without_Token_Should_Return_Unauthorized()
    {
        var client = _factory.CreateClient();
        using var multipart = new MultipartFormDataContent();
        multipart.Add(new ByteArrayContent([]), "file", "test.png");

        var response = await client.PostAsync("/auth/profile-picture", multipart);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
