using FluentValidation;
using FSH.Modules.Profile.Contracts.v1.Subdivisions;

namespace FSH.Modules.Profile.Features.v1.Subdivisions.UpdateSubdivision;

public sealed class UpdateSubdivisionCommandValidator : AbstractValidator<UpdateSubdivisionCommand>
{
    public UpdateSubdivisionCommandValidator()
    {
        RuleFor(x => x.SubdivisionId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Description).MaximumLength(1024);
        RuleFor(x => x.TypeSubdivision).MaximumLength(512);
    }
}
