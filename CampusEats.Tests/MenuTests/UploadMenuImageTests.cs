using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using CampusEats.Api.Data;
using CampusEats.Api.Features.Menu.UploadMenuImage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;

namespace CampusEats.Tests.MenuTests;

public class UploadMenuImageTests
{
    [Fact]
    public async Task Handle_Should_Save_File_And_Return_Url()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "campuseats-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        var env = new TestWebHostEnvironment
        {
            WebRootPath = tempRoot,
            WebRootFileProvider = new PhysicalFileProvider(tempRoot)
        };

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost");

        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = httpContext
        };
        
        await using var ms = new MemoryStream(Encoding.UTF8.GetBytes("fake-image-content"));
        var cmd = new UploadMenuImageCommand(
            "test.png",
            "image/png",
            ms.Length,
            ms
        );

        var handler = new UploadMenuImageHandler(env, httpContextAccessor);

        var result = await handler.Handle(cmd, CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Url));
        Assert.Contains("/menu-images/", result.Url);

        var uri = new Uri(result.Url);
        var savedPath = Path.Combine(tempRoot, uri.AbsolutePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(savedPath));
    }

    [Fact]
    public async Task Handle_Should_Throw_When_File_Empty()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "campuseats-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        var env = new TestWebHostEnvironment
        {
            WebRootPath = tempRoot,
            WebRootFileProvider = new PhysicalFileProvider(tempRoot)
        };

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost");

        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = httpContext
        };
        
        await using var ms = new MemoryStream(Array.Empty<byte>());

        var cmd = new UploadMenuImageCommand(
            "empty.png",
            "image/png",
            0,
            ms
        );

        var handler = new UploadMenuImageHandler(env, httpContextAccessor);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Should_Use_Default_Extension_When_Missing()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "campuseats-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        var env = new TestWebHostEnvironment
        {
            WebRootPath = tempRoot,
            WebRootFileProvider = new PhysicalFileProvider(tempRoot)
        };

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost");

        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = httpContext
        };
        
        await using var ms = new MemoryStream(Encoding.UTF8.GetBytes("fake-image-content"));
        
        // Filename without extension
        var cmd = new UploadMenuImageCommand(
            "noextension",
            "image/jpeg",
            ms.Length,
            ms
        );

        var handler = new UploadMenuImageHandler(env, httpContextAccessor);

        var result = await handler.Handle(cmd, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Contains(".jpg", result.Url);
    }

    [Fact]
    public async Task Handle_Should_Create_Directory_If_Not_Exists()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "campuseats-tests", Guid.NewGuid().ToString("N"));
        // Create temp root but not the menu-images subdirectory
        Directory.CreateDirectory(tempRoot);

        var env = new TestWebHostEnvironment
        {
            WebRootPath = tempRoot,
            WebRootFileProvider = new PhysicalFileProvider(tempRoot)
        };

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "http";
        httpContext.Request.Host = new HostString("example.com");

        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = httpContext
        };
        
        await using var ms = new MemoryStream(Encoding.UTF8.GetBytes("image-data"));
        var cmd = new UploadMenuImageCommand(
            "image.png",
            "image/png",
            ms.Length,
            ms
        );

        var handler = new UploadMenuImageHandler(env, httpContextAccessor);

        var result = await handler.Handle(cmd, CancellationToken.None);

        Assert.NotNull(result);
        var uploadsDir = Path.Combine(tempRoot, "menu-images");
        Assert.True(Directory.Exists(uploadsDir));
    }

    [Fact]
    public async Task Handle_Should_Copy_Stream_Content_To_File()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "campuseats-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        var env = new TestWebHostEnvironment
        {
            WebRootPath = tempRoot,
            WebRootFileProvider = new PhysicalFileProvider(tempRoot)
        };

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("test.local");

        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = httpContext
        };
        
        var testContent = "This is test image content with some data";
        await using var ms = new MemoryStream(Encoding.UTF8.GetBytes(testContent));
        
        var cmd = new UploadMenuImageCommand(
            "test.jpg",
            "image/jpeg",
            ms.Length,
            ms
        );

        var handler = new UploadMenuImageHandler(env, httpContextAccessor);

        var result = await handler.Handle(cmd, CancellationToken.None);

        // Verify file content matches
        var uri = new Uri(result.Url);
        var savedPath = Path.Combine(tempRoot, uri.AbsolutePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        var savedContent = await File.ReadAllTextAsync(savedPath);
        
        Assert.Equal(testContent, savedContent);
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = "";
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

public class UploadMenuImageEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public UploadMenuImageEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("TestDb"));
            });
        });

        _client = _factory.CreateClient();
    }

    private static MultipartFormDataContent BuildMultipartWithFile(
        string fieldName,
        string fileName,
        string content,
        string contentType = "image/png")
    {
        var multi = new MultipartFormDataContent();
        var bytes = Encoding.UTF8.GetBytes(content);
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        multi.Add(fileContent, fieldName, fileName);
        return multi;
    }

    [Fact]
    public async Task UploadMenuImage_MissingContentType_Should_Return_BadRequest()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/menu/images")
        {
            Content = new StringContent("not-multipart")
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadMenuImage_MissingFile_Should_Return_BadRequest()
    {
        using var multi = new MultipartFormDataContent();
        var response = await _client.PostAsync("/api/menu/images", multi);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadMenuImage_ValidFile_Should_Return_Ok_And_Url()
    {
        using var content = BuildMultipartWithFile("file", "test.png", "fake-image-content");
        var response = await _client.PostAsync("/api/menu/images", content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var dto = await response.Content.ReadFromJsonAsync<UploadMenuImageResult>();
        Assert.NotNull(dto);
        Assert.False(string.IsNullOrWhiteSpace(dto!.Url));
        Assert.Contains("/menu-images/", dto.Url);
    }
}