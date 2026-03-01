// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Server.OnPray.HealNearOnPray;
using Content.Goobstation.Shared.Religion.Nullrod;
using Content.Medical.Common.Damage;
using Content.Shared._EinsteinEngines.Silicon.Components;
using Content.Medical.Common.Targeting;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.Ghost;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Revenant.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Goobstation.Server.Religion.OnPray.HealNearOnPray;

public sealed partial class HealNearOnPraySystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;

    private EntityQuery<SpectralComponent> _spectralQuery;
    private EntityQuery<CorporealComponent> _corporealQuery;

    private HashSet<Entity<MobStateComponent>> _targets = new();

    public override void Initialize()
    {
        base.Initialize();

        _spectralQuery = GetEntityQuery<SpectralComponent>();
        _corporealQuery = GetEntityQuery<CorporealComponent>();

        SubscribeLocalEvent<HealNearOnPrayComponent, AlternatePrayEvent>(OnPray);
    }

    private void OnPray(EntityUid uid, HealNearOnPrayComponent comp, ref AlternatePrayEvent args)
    {
        _targets.Clear();
        _lookup.GetEntitiesInRange(Transform(args.User).Coordinates, comp.Range, _targets);
        _targets.RemoveWhere(entity => !_examine.InRangeUnOccluded(uid, entity, comp.Range));

        foreach (var entity in _targets)
        {
            if (_mobState.IsDead(entity.AsNullable())
                || HasComp<SiliconComponent>(entity))
                continue;

            // if its a ghost and its not in corporeal form then skip
            if (_spectralQuery.HasComp(entity) && !_corporealQuery.HasComp(entity))
                continue;

            var ev = new DamageUnholyEvent(entity, args.User);
            RaiseLocalEvent(entity, ref ev);

            if (ev.ShouldTakeHoly)
            {
                _damageable.ChangeDamage(entity.Owner, comp.Damage, targetPart: TargetBodyPart.All, splitDamage: SplitDamageBehavior.SplitEnsureAll);
                Spawn(comp.DamageEffect, Transform(entity).Coordinates);
                _audio.PlayPvs(comp.SizzleSoundPath, entity, new AudioParams(-2f, 1f, SharedAudioSystem.DefaultSoundRange, 1f, false, 0f)); //This should be safe to keep in the loop as this sound will never consistently play on multiple entities.
            }
            else
            {
                _damageable.ChangeDamage(entity.Owner, comp.Healing, targetPart: TargetBodyPart.All, ignoreBlockers: true, splitDamage: SplitDamageBehavior.SplitEnsureAll);
                Spawn(comp.HealEffect, Transform(entity).Coordinates);
            }
        }

        _audio.PlayPvs(comp.HealSoundPath, uid, new AudioParams(-2f, 1f, SharedAudioSystem.DefaultSoundRange, 1f, false, 0f)); //Played outside the loop once at the source of the damage to prevent repeated sound-stacking.
    }
}
