using Robust.Shared.Prototypes;

namespace Content.Lavaland.Shared.Megafauna;

/// <summary>
/// Arguments that are used for Megafauna Actions and Conditions.
/// </summary>
public record struct MegafaunaCalculationBaseArgs(
    EntityUid Entity,
    IEntityManager EntityManager,
    IPrototypeManager PrototypeMan,
    ISawmill Logger,
    System.Random Random);
