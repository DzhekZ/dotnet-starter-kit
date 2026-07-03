using FluentValidation;
using FSH.Modules.Profile.Contracts.v1.Profiles.RemoveProfileImage;

namespace FSH.Modules.Profile.Features.v1.Profiles.RemoveProfileImage;

public sealed class RemoveProfileImageCommandValidator : AbstractValidator<RemoveProfileImageCommand>
{
    public RemoveProfileImageCommandValidator()
    {
        RuleFor(x => x.ProfileId).NotEmpty();
        RuleFor(x => x.ImageId).NotEmpty();
    }
}
