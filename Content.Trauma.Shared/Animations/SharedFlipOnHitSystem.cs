// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Standing;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Whitelist;

namespace Content.Trauma.Shared.Animations;

public abstract partial class SharedFlipOnHitSystem : EntitySystem
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    [Dependency] protected StandingStateSystem Standing = default!;
    [Dependency] protected StatusEffectsSystem Status = default!;

    protected static readonly TimeSpan Duration = TimeSpan.FromMilliseconds(1600);
    protected const string AnimationKey = "fliponhit";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FlipOnHitComponent, MeleeHitEvent>(OnHit);
        SubscribeLocalEvent<StatusEffectContainerComponent, DownedEvent>(OnDowned);

        SubscribeLocalEvent<FlippingStatusEffectComponent, StatusEffectRemovedEvent>(OnRemoved);
        SubscribeLocalEvent<FlippingStatusEffectComponent, StatusEffectAppliedEvent>(OnApplied);
        SubscribeLocalEvent<FlippingStatusEffectComponent, StatusEffectEndTimeUpdatedEvent>(OnUpdated);
    }

    private void OnUpdated(Entity<FlippingStatusEffectComponent> ent, ref StatusEffectEndTimeUpdatedEvent args)
    {
        PlayAnimation(args.Target);
    }

    private void OnApplied(Entity<FlippingStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        PlayAnimation(args.Target);
    }

    private void OnRemoved(Entity<FlippingStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (IsClientSide(ent))
            return;

        if (TerminatingOrDeleted(args.Target))
            return;

        if (Status.HasEffectComp<FlippingStatusEffectComponent>(args.Target))
            return;

        StopAnimation(args.Target);
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
        if (ent.Comp.LightAttackOnly && args.Direction != null)
            return;

        if (args.HitEntities.Count == 0)
            return;

        if (TryComp(ent, out ItemToggleComponent? itemToggle) && !itemToggle.Activated)
            return;

        if (Standing.IsDown(args.User))
            return;

        if (!ent.Comp.TriggerOnSelfHit && args.HitEntities.Contains(args.User))
            return;

        if (ent.Comp.HitWhitelist is { } whitelist &&
            args.HitEntities.All(x => _whitelist.IsWhitelistFail(whitelist, x)))
            return;

        Status.TryUpdateStatusEffectDuration(args.User, ent.Comp.StatusEffect, Duration);
    }

    protected abstract void PlayAnimation(EntityUid user);

    protected abstract void StopAnimation(EntityUid user);
}

[Serializable, NetSerializable]
public sealed class FlipOnHitEvent(NetEntity user) : EntityEventArgs
{
    public NetEntity User = user;
}

[Serializable, NetSerializable]
public sealed class FlipOnHitStopEvent(NetEntity user) : EntityEventArgs
{
    public NetEntity User = user;
}
