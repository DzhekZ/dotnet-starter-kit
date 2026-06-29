using Mediator;

namespace FSH.Modules.Profile.Contracts.v1.Positions;

public sealed record DeletePositionCommand(Guid PositionId) : ICommand<Unit>;
