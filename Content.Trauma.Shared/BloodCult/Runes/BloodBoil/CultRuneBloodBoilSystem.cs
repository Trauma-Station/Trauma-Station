// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.EntityEffects;
using Content.Shared.Examine;
using Content.Shared.Projectiles;
using Content.Shared.Random.Helpers;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Trauma.Shared.BloodCult.BloodBoilProjectile;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Trauma.Shared.BloodCult.Runes.BloodBoil;

public sealed partial class CultRuneBloodBoilSystem : EntitySystem
{
    [Dependency] private BloodCultSystem _cult = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private EntityQuery<BloodCultMemberComponent> _memberQuery = default!;

    [SubscribeLocalEvent]
    private void OnBloodBoilRuneInvoked(Entity<CultRuneBloodBoilComponent> ent, ref RuneInvokeEvent args)
    {
        var user = args.User;
        var targets = _cult.GetTargetsNearRune(ent, ent.Comp.TargetsLookupRange);
        // TODO MULTICULT: compare against the invoker's cult
        targets.RemoveWhere(entity => _memberQuery.HasComp(entity));
        targets.RemoveWhere(entity => !_examine.InRangeUnOccluded(ent, entity, ent.Comp.TargetsLookupRange));

        if (targets.Count == 0)
        {
            args.Popup = Loc.GetString("cult-blood-boil-rune-no-targets");
            return;
        }

        var list = targets.ToList();
        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));
        for (var i = 0; i < ent.Comp.ProjectileCount; i++)
        {
            var target = rand.Pick(list);
            _effects.ApplyEffects(target, ent.Comp.Effects, user: user);
            Shoot(ent, target);
        }

        _audio.PlayPredicted(ent.Comp.ActivationSound, ent, user, AudioParams.Default.WithMaxDistance(2f));
        args.Handled = true;
    }

    private void Shoot(Entity<CultRuneBloodBoilComponent> ent, EntityUid target)
    {
        var runeMapPos = _transform.GetMapCoordinates(ent);
        var targetMapPos = _transform.GetMapCoordinates(target);

        var proj = PredictedSpawnAtPosition(ent.Comp.ProjectilePrototype, Transform(ent).Coordinates);
        var direction = targetMapPos.Position - runeMapPos.Position;

        var boil = EnsureComp<BloodBoilProjectileComponent>(proj);
        boil.Target = target;
        Dirty(proj, boil);

        _gun.ShootProjectile(proj, direction, Vector2.Zero, ent, ent, ent.Comp.ProjectileSpeed);
    }
}
