using FluentValidation;
using FSH.Modules.Profile.Contracts.v1.Profiles.SetProfileThumbnail;

namespace FSH.Modules.Profile.Features.v1.Profiles.SetProfileThumbnail;

public sealed class SetProfileThumbnailCommandValidator : AbstractValidator<SetProfileThumbnailCommand>
{
    public SetProfileThumbnailCommandValidator()
    {
        RuleFor(x => x.ProfileId).NotEmpty();
        RuleFor(x => x.ImageId).NotEmpty();
    }
}
