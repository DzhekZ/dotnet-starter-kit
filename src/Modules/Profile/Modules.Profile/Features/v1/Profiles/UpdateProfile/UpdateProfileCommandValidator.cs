using FluentValidation;
using FSH.Modules.Profile.Contracts.v1.Profiles;

namespace FSH.Modules.Profile.Features.v1.Profiles.UpdateProfile;

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.ProfileId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(250);
        RuleFor(x => x.PersonnelNumber).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CodePerson).MaximumLength(50);
        RuleFor(x => x.Email).MaximumLength(100);
        RuleFor(x => x.Login).MaximumLength(50);
        RuleFor(x => x.AdSid).MaximumLength(50);
        RuleFor(x => x.Sex).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TypeEmployment).MaximumLength(50);
        RuleFor(x => x.Staffing).MaximumLength(900);
        RuleFor(x => x.City).MaximumLength(50);
        RuleFor(x => x.Category).MaximumLength(50);
        RuleFor(x => x.PhoneMobile).MaximumLength(30);
        RuleFor(x => x.PhoneWork).MaximumLength(50);
        RuleFor(x => x.Division).MaximumLength(50);
        RuleFor(x => x.Place).MaximumLength(50);
        RuleFor(x => x.WtHcmId).MaximumLength(50);
        ////RuleFor(x => x.PersonnelNumber).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Information).MaximumLength(4000);
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.PositionId).NotEmpty();
        RuleFor(x => x.SubdivisionId).NotEmpty();
        RuleFor(x => x.HierarchyId).NotEmpty();
    }
}
