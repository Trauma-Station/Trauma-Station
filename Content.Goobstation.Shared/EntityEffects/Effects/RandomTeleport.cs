using Content.Goobstation.Shared.Teleportation.Systems;
using Content.Shared.Destructible.Thresholds;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.EntityEffects.Effects;

public sealed partial class RandomTeleport : EntityEffectBase<RandomTeleport>
{
    /// <summary>
    /// Up to how far to teleport the user in tiles.
    /// </summary>
    [DataField]
    public MinMax Radius = new MinMax(5, 20);

    /// <summary>
    /// How many times to try to pick the destination. Larger number means the teleport is more likely to be safe.
    /// </summary>
    [DataField]
    public int TeleportAttempts = 10;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null; // TODO
}

public sealed class RandomTeleportEffectSystem : EntityEffectSystem<TransformComponent, RandomTeleport>
{
    [Dependency] private readonly SharedRandomTeleportSystem _teleport = default!;

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<RandomTeleport> args)
    {
        _teleport.RandomTeleport(ent, args.Effect.Radius, args.Effect.TeleportAttempts);
    }
}
