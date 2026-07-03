using FluentValidation;
using FSH.Modules.Profile.Contracts.v1.Subdivisions;

namespace FSH.Modules.Profile.Features.v1.Subdivisions.CreateSubdivision;

public sealed class CreateSubdivisionCommandValidator : AbstractValidator<CreateSubdivisionCommand>
{
    public CreateSubdivisionCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Description).MaximumLength(1024);
        RuleFor(x => x.TypeSubdivision).MaximumLength(512);
    }
}
