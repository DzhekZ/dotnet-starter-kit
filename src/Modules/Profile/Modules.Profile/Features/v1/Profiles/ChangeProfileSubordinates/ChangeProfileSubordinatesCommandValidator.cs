using FluentValidation;
using FSH.Modules.Profile.Contracts.v1.Profiles;

namespace FSH.Modules.Profile.Features.v1.Profiles.ChangeProfileSubordinates;

public sealed class ChangeProfileSubordinatesCommandValidator : AbstractValidator<ChangeProfileSubordinatesCommand>
{
    public ChangeProfileSubordinatesCommandValidator()
    {
        RuleFor(x => x.ProfileId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0);
    }
}
