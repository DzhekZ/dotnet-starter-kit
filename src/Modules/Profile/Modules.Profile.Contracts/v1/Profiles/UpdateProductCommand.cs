using Mediator;

namespace FSH.Modules.Profile.Contracts.v1.Profiles;

public sealed record UpdateProductCommand(
    Guid ProductId,
    string Name,
    string? Description,
    Guid BrandId,
    Guid CategoryId,
    bool IsActive) : ICommand<Guid>;
