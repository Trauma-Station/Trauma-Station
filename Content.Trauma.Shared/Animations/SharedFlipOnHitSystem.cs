// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Standing;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Animations;

public abstract class SharedFlipOnHitSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly StandingStateSystem _standingState = default!;
    [Dependency] protected readonly StatusEffectsSystem Status = default!;

    protected static readonly TimeSpan Duration = TimeSpan.FromMilliseconds(1600);
    protected const string AnimationKey = "flip";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FlipOnHitComponent, MeleeHitEvent>(OnHit);
        SubscribeLocalEvent<StatusEffectContainerComponent, DownedEvent>(OnDowned);
    }

    private void OnDowned(Entity<StatusEffectContainerComponent> ent, ref DownedEvent args)
    {
        if (!Status.TryEffectsWithComp<FlippingStatusEffectComponent>(ent, out var effects))
            return;

        foreach (var effect in effects)
        {
            PredictedQueueDel(effect);
        }
    }

    private void OnHit(Entity<FlipOnHitComponent> ent, ref MeleeHitEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (ent.Comp.LeftClickOnly && args.Direction != null)
            return;

        if (args.HitEntities.Count == 0)
            return;

        if (TryComp(ent, out ItemToggleComponent? itemToggle) && !itemToggle.Activated)
            return;

        if (_standingState.IsDown(args.User))
            return;

        Status.TryUpdateStatusEffectDuration(args.User, ent.Comp.StatusEffect, Duration);
    }
}
