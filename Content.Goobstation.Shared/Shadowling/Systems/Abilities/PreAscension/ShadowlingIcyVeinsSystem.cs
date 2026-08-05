// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Shadowling.Components;
using Content.Goobstation.Shared.Shadowling.Components.Abilities.PreAscension;
using Content.Shared.Actions;
using Content.Shared.Coordinates;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.Shadowling.Systems.Abilities.PreAscension;

/// <summary>
/// This handles Icy Veins logic. An AOE ability that lowers the temperature
/// of targets nearby and paralyzes them for a very short amount.
/// </summary>
public sealed partial class ShadowlingIcyVeinsSystem : EntitySystem
{
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedTemperatureSystem _temp = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private EntityQuery<TemperatureComponent> _tempQuery = default!;

    private HashSet<Entity<TemperatureComponent>> _targets = new();

    [SubscribeLocalEvent]
    private void OnStartup(Entity<ShadowlingIcyVeinsComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.ActionEnt, ent.Comp.ActionId);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<ShadowlingIcyVeinsComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEnt);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        // Target's temperature adjusts to normal in a minute or so after the effect.
        // That means, they won't take lots of damage from this ability but they will be slowed down.
        var query = EntityQueryEnumerator<IcyVeinsTargetComponent>();
        foreach (var ent in query)
        {
            if (now < ent.Comp.NextDecrease)
                continue;

            ent.Comp.NextDecrease = now + ent.Comp.DecreaseDelay;
            if (!_tempQuery.TryComp(ent, out var temp) || temp.Temperature <= ent.Comp.MinTargetTemperature)
            {
                RemCompDeferred(ent, ent.Comp);
                continue;
            }

            // *magically lowers your temperature* nothing personnel kid
            _temp.ChangeHeat((ent, temp), -ent.Comp.TempDecrease * temp.HeatCapacity);
        }
    }

    [SubscribeLocalEvent]
    private void OnIcyVeins(Entity<ShadowlingIcyVeinsComponent> ent, ref IcyVeinsEvent args)
    {
        if (args.Handled)
            return;

        var coords = Transform(ent).Coordinates;
        _targets.Clear();
        _lookup.GetEntitiesInRange(coords, ent.Comp.Range, _targets);
        foreach (var target in _targets)
        {
            TryIcyVeins(target, ent);
        }

        var effectEnt = PredictedSpawnAttachedTo(ent.Comp.IcyVeinsEffect, ent.Owner.ToCoordinates());
        var sound = ent.Comp.IcyVeinsSound;
        _audio.PlayPredicted(sound, ent, ent, sound.Params.WithVolume(-1f));
        args.Handled = true;
    }

    private void TryIcyVeins(EntityUid target, Entity<ShadowlingIcyVeinsComponent> ent)
    {
        if (!HasComp<MobStateComponent>(target)
            || HasComp<ShadowlingComponent>(target)
            || HasComp<ThrallComponent>(target))
            return;

        EnsureComp<IcyVeinsTargetComponent>(target);
        _popup.PopupEntity(Loc.GetString("shadowling-icy-veins-activated"), target, target, PopupType.MediumCaution);

        _stun.TryAddParalyzeDuration(target, ent.Comp.ParalyzeTime);
    }
}
