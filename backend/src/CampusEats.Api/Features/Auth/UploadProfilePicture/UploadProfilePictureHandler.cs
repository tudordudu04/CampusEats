using MediatR;

namespace CampusEats.Api.Features.Auth.UploadProfilePicture;

public class UploadProfilePictureHandler(IWebHostEnvironment env, IHttpContextAccessor httpContextAccessor) : IRequestHandler<UploadProfilePictureCommand,UploadProfilePictureResult>
{
    public async Task<UploadProfilePictureResult> Handle(UploadProfilePictureCommand request,
        CancellationToken cancellationToken)
    {
        var webRoot = env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var uploadsRoot = Path.Combine(webRoot, "profile-images");
        
        Directory.CreateDirectory(uploadsRoot);
        
        var extension = Path.GetExtension(request.FileName);
        if(string.IsNullOrWhiteSpace(extension)) extension = "jpg";
        
        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsRoot, fileName);

        await using (var fileStream = File.Create(filePath))
        {
            await request.FileStream.CopyToAsync(fileStream,cancellationToken);
        }
        var httpContext = httpContextAccessor.HttpContext;
        var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
        var url = $"{baseUrl}/profile-images/{fileName}";
        
        return new UploadProfilePictureResult(url);
    }
    
}