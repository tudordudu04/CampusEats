using MediatR;
namespace CampusEats.Api.Features.Auth.UploadProfilePicture;

public record UploadProfilePictureCommand(Stream FileStream, string FileName) : IRequest<UploadProfilePictureResult>;
public record UploadProfilePictureResult(string ProfilePictureUrl);