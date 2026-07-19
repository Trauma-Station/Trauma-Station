// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Administration.Logs;
using Content.Shared.Body.Components;
using Content.Shared.Database;
using Content.Shared.EntityEffects;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Vampires.Haemomancer;

public abstract partial class SharedActiveBloodLeecherSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private ISharedAdminLogManager _admin = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    private readonly HashSet<Entity<BloodstreamComponent>> _drainable = new();

    private static readonly EntProtoId BeamProto = "BloodBeam";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActiveBloodLeecherComponent, ComponentStartup>(OnMapInit);
        SubscribeLocalEvent<ActiveBloodLeecherComponent, ComponentShutdown>(OnShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        var eqe = EntityQueryEnumerator<ActiveBloodLeecherComponent>();
        while (eqe.MoveNext(out var uid, out var comp))
        {
            if (now < comp.NextUpdate)
                continue;

            var attemptEv = new BloodLeecherAttemptEvent(comp.BloodRequired);
            RaiseLocalEvent(uid, ref attemptEv);
            if (attemptEv.Cancelled)
            {
                _popup.PopupEntity("You don't have enough power to leech! You must stop leeching.", uid, PopupType.MediumCaution);

                comp.NextUpdate = _timing.CurTime + comp.UpdateRate;
                Dirty(uid, comp);
                continue;
            }

            _drainable.Clear();
            var counter = 0;
            _lookup.GetEntitiesInRange(Transform(uid).Coordinates, comp.Range, _drainable);
            foreach (var drain in _drainable)
            {
                // Skip self, duh
                if (drain.Owner == uid)
                    continue;

                // Ensures we only have a limited amount of targets
                if (counter >= comp.MaxEntities)
                    break;

                if (comp.TargetEffects is not { } targetEffects)
                    continue;

                _effects.ApplyEffects(drain, targetEffects);
                CreateBeam(uid, drain, BeamProto);

                counter++;
            }

            // Apply effects only if we have had targets near us
            var count = _drainable.Count;
            if (count > 0)
            {
                if (comp.UserEffects is not { } user)
                    continue;

                _effects.ApplyEffects(uid, user, count);
            }

            comp.NextUpdate = _timing.CurTime + comp.UpdateRate;
            Dirty(uid, comp);
        }
    }

    private void OnMapInit(Entity<ActiveBloodLeecherComponent> ent, ref ComponentStartup args)
    {
        // phantom audio if predicted
        if (_net.IsServer && _audio.PlayPvs(ent.Comp.Music, ent)?.Entity is { } audio)
        {
            ent.Comp.MusicEntity = audio;
        }

        _admin.Add(LogType.Vampire, LogImpact.Medium, $"User {ent.Owner} has initiated the Blood Bringer's Rite action");

        ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.UpdateRate;
        Dirty(ent);
    }

    private void OnShutdown(Entity<ActiveBloodLeecherComponent> ent, ref ComponentShutdown args)
    {
        _admin.Add(LogType.Vampire, LogImpact.Medium, $"User {ent.Owner} has stopped the Blood Bringer's Rite action");

        _audio.Stop(ent.Comp.MusicEntity);
    }

    /// <summary>
    /// Creates a beam server-side from user to target.
    /// </summary>
    public virtual void CreateBeam(EntityUid user, EntityUid target, EntProtoId beamProto) { }
}

/// <summary>
/// Raised on the user to check if they can continue blood leeching.
/// </summary>
[ByRefEvent]
public record struct BloodLeecherAttemptEvent(int BloodRequired, bool Cancelled = false);
