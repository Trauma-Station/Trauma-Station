// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Foldable;
using Content.Shared.Projectiles;
using Content.Shared.Salvage.Fulton;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Salvage;

/// <summary>
/// The fulton surface-to-air recovery system, I'm familiar with the theory.
/// </summary>
public sealed partial class FultonProjectileSystem : EntitySystem
{
    [Dependency] private FoldableSystem _foldable = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedFultonSystem _fulton = default!;
    [Dependency] private EntityQuery<FultonComponent> _fultonQuery = default!;
    [Dependency] private EntityQuery<ProjectileComponent> _projQuery = default!;

    [SubscribeLocalEvent]
    private void OnProjectileHit(Entity<FultonProjectileComponent> ent, ref ProjectileHitEvent args)
    {
        var target = args.Target;
        if (!_timing.IsFirstTimePredicted || // audio + fulton appearance dont play nice
            !_projQuery.TryComp(ent, out var proj) ||
            proj.Weapon is not { } weapon ||
            !_fultonQuery.TryComp(weapon, out var fulton))
            return;

        if (!_fulton.CanApplyFulton(target, fulton))
        {
            // projectiles are predicted by all clients so it can be local
            _audio.PlayLocal(ent.Comp.PopSound, target, null);
            return;
        }

        if (Deleted(fulton.Beacon) || _foldable.IsFolded(fulton.Beacon.Value))
        {
            // same prediction as regular fulton interaction
            if (_net.IsServer || !fulton.HasBeacon)
            {
                if (fulton.HasBeacon)
                {
                    fulton.HasBeacon = false;
                    fulton.Beacon = null;
                    Dirty(weapon, fulton);
                }
                _audio.PlayLocal(ent.Comp.PopSound, target, null);
                return;
            }
        }

        if (EnsureComp<FultonedComponent>(target, out var fultoned))
            return;

        // project 0 api
        fultoned.Beacon = fulton.Beacon;
        fultoned.NextFulton = _timing.CurTime + fulton.FultonDuration;
        fultoned.FultonDuration = fulton.FultonDuration;
        fultoned.Removeable = fulton.Removeable;
        Dirty(target, fultoned);
        _audio.PlayLocal(fulton.FultonSound, target, null);
    }
}
