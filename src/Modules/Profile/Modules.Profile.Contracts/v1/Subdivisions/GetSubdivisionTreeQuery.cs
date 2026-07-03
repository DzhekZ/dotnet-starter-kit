using FSH.Modules.Profile.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Profile.Contracts.v1.Subdivisions;

public sealed record GetSubdivisionTreeQuery : IQuery<IReadOnlyList<SubdivisionTreeNodeDto>>;
