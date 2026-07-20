// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Components;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Blade;
using Content.Trauma.Shared.Heretic.Rituals;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Trauma.Shared.Heretic.Systems.PathSpecific.Blade;

public sealed partial class InfusedItemSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;

    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedRatvarianLanguageSystem _language = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedItemSystem _item = default!;
    [Dependency] private SharedMansusGraspSystem _grasp = default!;
    [Dependency] private SharedHereticSystem _heretic = default!;
    [Dependency] private HereticRitualEffectSystem _effects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MansusInfusedComponent, MeleeHitEvent>(OnInfusedMeleeHit,
            after: new[] { typeof(HereticBladeSystem) });
    }

    [SubscribeLocalEvent]
    private void OnInfusedExamine(Entity<MansusInfusedComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("mansus-infused-item-examine"));
    }

    [SubscribeLocalEvent]
    private void OnInfusedInteract(Entity<MansusInfusedComponent> ent, ref InteractHandEvent args)
    {
        var target = args.User;

        if (_heretic.IsHereticOrGhoul(target))
            return;

        if (HasComp<StatusEffectsComponent>(target))
        {
            _audio.PlayPvs(new SoundPathSpecifier("/Audio/Items/welder.ogg"), target);
            _stun.TryUpdateParalyzeDuration(target, TimeSpan.FromSeconds(5f));
            _language.DoRatvarian(target, TimeSpan.FromSeconds(10f), true);
        }

        _hands.TryDrop(target, Transform(target).Coordinates);
        SpendInfusionCharges(ent);
    }

    [SubscribeLocalEvent]
    private void OnThrowHit(Entity<MansusInfusedComponent> ent, ref ThrowDoHitEvent args)
    {
        if (args.Component.Thrower is { } user && HasComp<DamageOtherOnHitComponent>(ent))
            ApplyInfusionEffects(ent, user, args.Target);
    }

    private void OnInfusedMeleeHit(Entity<MansusInfusedComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || args.HitEntities.Count == 0 || args.Direction != null)
            return;

        ApplyInfusionEffects(ent, args.User, args.HitEntities[0]);
    }

    private void ApplyInfusionEffects(Entity<MansusInfusedComponent> ent, EntityUid user, EntityUid target)
    {
        if (!_heretic.TryGetHereticComponent(user, out var heretic, out var mind) ||
            heretic.CurrentPath is not { } path)
            return;

        if (target == user)
            return;

        if (!HasComp<StatusEffectsComponent>(target) || !TryComp(target, out MobStateComponent? mobState) ||
            mobState.CurrentState == MobState.Dead)
            return;

        var raiser = EnsureComp<HereticRitualRaiserComponent>(ent);
        raiser.Blackboard.Clear();
        raiser.Blackboard[SharedHereticRitualSystem.Performer] = user;
        raiser.Blackboard[SharedHereticRitualSystem.Mind] = mind;

        _effects.TryApplyEffect(target, ent.Comp.InfusedHitEffect, (ent, raiser), user);
        _grasp.ApplyMark(target, path, heretic.PassiveLevel);

        raiser.Blackboard.Clear();
        SpendInfusionCharges(ent);
    }

    private void SpendInfusionCharges(Entity<MansusInfusedComponent> ent)
    {
        if (_net.IsClient)
            return;

        ent.Comp.AvailableCharges -= 1;
        if (ent.Comp.AvailableCharges <= 0)
            RemComp(ent.Owner, ent.Comp);
    }

    [SubscribeLocalEvent]
    private void OnInfusedStartup(Entity<MansusInfusedComponent> ent, ref ComponentStartup args)
    {
        _appearance.SetData(ent, InfusedBladeVisuals.Infused, true);
        _item.SetHeldPrefix(ent, ent.Comp.HeldPrefix);
    }

    [SubscribeLocalEvent]
    private void OnInfusedShutdown(Entity<MansusInfusedComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        _appearance.SetData(ent, InfusedBladeVisuals.Infused, false);
        _item.SetHeldPrefix(ent, null);
    }
}
