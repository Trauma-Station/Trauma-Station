// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Shadowling.Components;
using Content.Goobstation.Shared.Shadowling.Components.Abilities.CollectiveMind;
using Content.Shared.Actions;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;

namespace Content.Goobstation.Shared.Shadowling.Systems.Abilities.CollectiveMind;

/// <summary>
/// This handles the Sonic Screech ability logic.
/// Sonic Screech "confuses" and "deafens" (flash effect + tinnitus sound) nearby people, damages windows, and stuns silicons/borgs. All in one pack!
/// </summary>
public sealed partial class ShadowlingSonicScreechSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private EntityQuery<HumanoidProfileComponent> _humanoidQuery = default!;
    [Dependency] private EntityQuery<MobStateComponent> _mobQuery = default!;

    private readonly HashSet<Entity<DamageableComponent>> _targets = new();

    [SubscribeLocalEvent]
    private void OnStartup(Entity<ShadowlingSonicScreechComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.ActionEnt, ent.Comp.ActionId);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<ShadowlingSonicScreechComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEnt);
    }

    // TODO: how many of these fucking copypasted screech systems are there
    [SubscribeLocalEvent]
    private void OnSonicScreech(Entity<ShadowlingSonicScreechComponent> ent, ref SonicScreechEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        _popup.PopupEntity(Loc.GetString("shadowling-sonic-screech-complete"), ent, ent, PopupType.Medium);
        _audio.PlayPredicted(ent.Comp.ScreechSound, ent, ent);

        var coords = Transform(ent).Coordinates;
        var effectEnt = PredictedSpawnAtPosition(ent.Comp.SonicScreechEffect, coords);
        _transform.SetParent(effectEnt, ent);

        _targets.Clear();
        _lookup.GetEntitiesInRange(coords, ent.Comp.Range, _targets);
        foreach (var target in _targets)
        {
            // TODO: audio occlusion check...

            if (_tag.HasTag(target, ent.Comp.WindowTag))
            {
                _damage.ChangeDamage(target.AsNullable(), ent.Comp.WindowDamage, true);
                continue;
            }

            if (!_mobQuery.HasComp(target))
                continue;

            if (_whitelist.IsWhitelistPass(ent.Comp.Blacklist, target))
                continue;

            if (_whitelist.IsWhitelistPass(ent.Comp.SiliconWhitelist, target))
            {
                _stun.TryAddParalyzeDuration(target.Owner, ent.Comp.SiliconStunTime);
                continue;
            }

            if (_humanoidQuery.HasComp(target))
                PredictedSpawnAtPosition(ent.Comp.ProtoFlash, Transform(target).Coordinates);
        }
    }
}
