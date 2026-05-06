using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.EntityEffects.Vampires;

/// <summary>
/// Effect that spawns a shadow clone at your location.
/// </summary>
public sealed partial class SpawnShadowClone : EntityEffectBase<SpawnShadowClone>
{
    /// <summary>
    /// How many clones to spawn.
    /// </summary>
    [DataField]
    public int Amount = 1;
}

public abstract class SharedSpawnShadowCloneEffectSystem : EntityEffectSystem<TransformComponent, SpawnShadowClone>
{
    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<SpawnShadowClone> args)
    {
        var effect = args.Effect;
        var amount = effect.Amount * (int)args.Scale;

        SpawnShadowClones(ent.Owner, amount);
    }

    /// <summary>
    /// Virtual function to spawn the shadow clones via the server.
    /// </summary>
    /// <param name="original"></param>
    /// <param name="amount"></param>
    protected virtual void SpawnShadowClones(EntityUid original, int amount) { }
}
