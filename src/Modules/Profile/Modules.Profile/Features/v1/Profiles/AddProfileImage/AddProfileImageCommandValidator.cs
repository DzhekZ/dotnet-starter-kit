using FluentValidation;
using FSH.Modules.Profile.Contracts.v1.Profiles.AddProfileImage;

namespace FSH.Modules.Profile.Features.v1.Profiles.AddProfileImage;

public sealed class AddProfileImageCommandValidator : AbstractValidator<AddProfileImageCommand>
{
    public AddProfileImageCommandValidator()
    {
        RuleFor(x => x.ProfileId).NotEmpty();
        RuleFor(x => x.Url).NotEmpty().MaximumLength(2048);
    }
}
