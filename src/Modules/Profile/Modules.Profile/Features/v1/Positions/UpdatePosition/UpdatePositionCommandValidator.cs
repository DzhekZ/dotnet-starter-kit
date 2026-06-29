using FluentValidation;
using FSH.Modules.Profile.Contracts.v1.Positions;

namespace FSH.Modules.Profile.Features.v1.Positions.UpdatePosition;

public sealed class UpdatePositionCommandValidator : AbstractValidator<UpdatePositionCommand>
{
    public UpdatePositionCommandValidator()
    {
        RuleFor(x => x.PositionId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Description).MaximumLength(1024);
    }
}
