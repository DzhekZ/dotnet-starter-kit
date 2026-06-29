using Mediator;

namespace FSH.Modules.Profile.Contracts.v1.Positions;

public sealed record RestorePositionCommand(Guid PositionId) : ICommand<Guid>;
