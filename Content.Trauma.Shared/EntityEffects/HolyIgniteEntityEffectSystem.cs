// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.EntityEffects;
using Content.Trauma.Shared.Chaplain;
using Content.Trauma.Shared.Chaplain.Components;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// This raises an extinguish event on a given entity, reducing FireStacks.
/// The amount of FireStacks reduced is modified by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class HolyExtinguishEntityEffectSystem : EntityEffectSystem<HolyFlammableComponent, HolyExtinguish>
{
    protected override void Effect(Entity<HolyFlammableComponent> entity, ref EntityEffectEvent<HolyExtinguish> args)
    {
        var ev = new HolyExtinguishEvent
        {
            FireStacksAdjustment = args.Effect.FireStacksAdjustment * args.Scale,
        };
        RaiseLocalEvent(entity, ref ev);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class HolyExtinguish : EntityEffectBase<HolyExtinguish>
{
    /// <summary>
    ///     Amount of FireStacks reduced.
    /// </summary>
    [DataField]
    public float FireStacksAdjustment = -1.5f;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("entity-effect-guidebook-extinguish-reaction", ("chance", Probability));
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
