namespace CampusEats.Api.Features.Menu.UploadMenuImage;

using MediatR;

public record UploadMenuImageCommand(
    string FileName,
    string ContentType,
    long Length,
    Stream FileStream
) : IRequest<UploadMenuImageResult>;

public sealed class UploadMenuImageResult(string url)
{
    public string Url { get; } = url;
}