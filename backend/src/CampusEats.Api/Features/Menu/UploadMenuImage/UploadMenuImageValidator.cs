namespace CampusEats.Api.Features.Menu.UploadMenuImage;

using FluentValidation;

public class UploadMenuImageValidator : AbstractValidator<UploadMenuImageCommand>
{
    private static readonly string[] AllowedContentTypes =
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif"
    };

    public UploadMenuImageValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.Length)
            .GreaterThan(0)
            .LessThanOrEqualTo(5 * 1024 * 1024); // 5 MB

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .Must(ct => AllowedContentTypes.Contains(ct))
            .WithMessage("Unsupported image type.");
    }
}