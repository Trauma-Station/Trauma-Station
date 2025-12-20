using Content.Goobstation.Shared.Religion;
using Content.Shared.EntityEffects;
using Content.Trauma.Shared.Chaplain;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Server.EntityEffects;

/// <summary>
/// This raises an the Ignite event on a given entity.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class HolyIgniteEntityEffectSystem : EntityEffectSystem<WeakToHolyComponent, HolyIgnite>
{
    protected override void Effect(Entity<WeakToHolyComponent> entity, ref EntityEffectEvent<HolyIgnite> args)
    {
        var ev = new HolyIgniteEvent
        {
            FireStacksAdjustment = args.Effect.Stacks,
        };
        RaiseLocalEvent(entity, ref ev);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class HolyIgnite : EntityEffectBase<HolyIgnite>
{
    /// <summary>
    ///     Amount of FireStacks improved.
    /// </summary>
    [DataField(required: true)]
    public float Stacks;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("entity-effect-guidebook-extinguish-reaction", ("chance", Probability));
}

