using FluentValidation;
using FSH.Modules.Profile.Contracts.v1.Profiles;

namespace FSH.Modules.Profile.Features.v1.Profiles.AdjustProfileStock;

public sealed class AdjustProfileStockCommandValidator : AbstractValidator<AdjustProfileStockCommand>
{
    public AdjustProfileStockCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Delta).NotEqual(0).WithMessage("Delta must be non-zero.");
    }
}
