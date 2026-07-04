using FluentValidation;
using FSH.Modules.Profile.Contracts.v1.Profiles.ReorderProfileImages;

namespace FSH.Modules.Profile.Features.v1.Profiles.ReorderProfileImages;

public sealed class ReorderProfileImagesCommandValidator : AbstractValidator<ReorderProfileImagesCommand>
{
    public ReorderProfileImagesCommandValidator()
    {
        RuleFor(x => x.Profiled).NotEmpty();
        RuleFor(x => x.OrderedImageIds).NotNull();
    }
}
