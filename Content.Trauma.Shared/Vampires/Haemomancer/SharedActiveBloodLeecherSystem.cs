// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Vampires.Haemomancer;

public abstract class SharedActiveBloodLeecherSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private readonly HashSet<Entity<VampireDrainableComponent>> _drainable = new();

    private static readonly EntProtoId BeamProto = "SuperchargedLightning"; // TODO: Change to actual

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActiveBloodLeecherComponent, MapInitEvent>(OnMapInit);
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

            var ev = new BloodLeecherAttemptEvent(comp.BloodRequired);
            RaiseLocalEvent(uid, ref ev);
            if (ev.Cancelled)
            {
                _popup.PopupClient("You don't have enough power to leech!", uid, PopupType.MediumCaution);

                comp.NextUpdate = _timing.CurTime + comp.UpdateRate;
                Dirty(uid, comp);
                continue;
            }

            _drainable.Clear();
            var counter = 0;
            _lookup.GetEntitiesInRange(Transform(uid).Coordinates, comp.Range, _drainable);
            foreach (var drain in _drainable)
            {
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

    private void OnMapInit(Entity<ActiveBloodLeecherComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.UpdateRate;
        Dirty(ent);
    }

    /// <summary>
    /// Creates a beam server-side from user to target.
    /// </summary>
    protected virtual void CreateBeam(EntityUid user, EntityUid target, EntProtoId beamProto) { }
}

/// <summary>
/// Raised on the user to check if they can continue blood leeching.
/// </summary>
[ByRefEvent]
public record struct BloodLeecherAttemptEvent(int BloodRequired, bool Cancelled = false);
