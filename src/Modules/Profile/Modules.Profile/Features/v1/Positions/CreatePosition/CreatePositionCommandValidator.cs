using FluentValidation;
using FSH.Modules.Profile.Contracts.v1.Positions;

namespace FSH.Modules.Profile.Features.v1.Positions.CreatePosition;

public sealed class CreatePositionCommandValidator : AbstractValidator<CreatePositionCommand>
{
    public CreatePositionCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Description).MaximumLength(1024);
    }
}
