// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Server.OnPray.HealNearOnPray;
using Content.Goobstation.Shared.Religion.Nullrod;
using Content.Medical.Common.Damage;
using Content.Medical.Common.Targeting;
using Content.Shared.Body;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.Ghost.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Revenant.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Goobstation.Server.Religion.OnPray.HealNearOnPray;

public sealed partial class HealNearOnPraySystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private EntityQuery<CorporealComponent> _corporealQuery = default!;
    [Dependency] private EntityQuery<SpectralComponent> _spectralQuery = default!;

    private HashSet<Entity<BodyComponent>> _targets = new();

    [SubscribeLocalEvent]
    private void OnPray(Entity<HealNearOnPrayComponent> ent, ref AlternatePrayEvent args)
    {
        _targets.Clear();
        _lookup.GetEntitiesInRange(Transform(args.User).Coordinates, ent.Comp.Range, _targets);

        foreach (var entity in _targets)
        {
            if (_mobState.IsDead(entity.Owner) ||
                !_examine.InRangeUnOccluded(ent.Owner, entity, ent.Comp.Range) ||
                _whitelist.IsWhitelistPass(ent.Comp.HealBlacklist, entity))
                continue;

            // if its a ghost and its not in corporeal form then skip
            if (_spectralQuery.HasComp(entity) && !_corporealQuery.HasComp(entity))
                continue;

            var ev = new DamageUnholyEvent(entity, args.User);
            RaiseLocalEvent(entity, ref ev);

            if (ev.ShouldTakeHoly)
            {
                _damageable.ChangeDamage(entity.Owner, ent.Comp.Damage, targetPart: TargetBodyPart.All, splitDamage: SplitDamageBehavior.SplitEnsureAll);
                Spawn(ent.Comp.DamageEffect, Transform(entity).Coordinates);
                _audio.PlayPvs(ent.Comp.SizzleSoundPath, entity, new AudioParams(-2f, 1f, SharedAudioSystem.DefaultSoundRange, 1f, false, 0f)); //This should be safe to keep in the loop as this sound will never consistently play on multiple entities.
            }
            else
            {
                _damageable.ChangeDamage(entity.Owner, ent.Comp.Healing, targetPart: TargetBodyPart.All, ignoreBlockers: true, splitDamage: SplitDamageBehavior.SplitEnsureAll);
                Spawn(ent.Comp.HealEffect, Transform(entity).Coordinates);
            }
        }

        _audio.PlayPvs(ent.Comp.HealSoundPath, ent, new AudioParams(-2f, 1f, SharedAudioSystem.DefaultSoundRange, 1f, false, 0f)); //Played outside the loop once at the source of the damage to prevent repeated sound-stacking.
    }
}
