// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.StatusEffectNew;
using Content.Trauma.Shared.Heretic.Components.StatusEffects;
using Content.Trauma.Shared.Heretic.Events;

namespace Content.Trauma.Shared.Heretic.Crucible.Systems;

public sealed class CrucibleSoulStatusEffectSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly PullingSystem _pull = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CrucibleSoulStatusEffectComponent, StatusEffectAppliedEvent>(OnApply);
        SubscribeLocalEvent<CrucibleSoulStatusEffectComponent, StatusEffectRemovedEvent>(OnRemove);

        SubscribeLocalEvent<CrucibleSoulRecallEvent>(OnRecall);
    }

    private void OnRecall(CrucibleSoulRecallEvent ev)
    {
        _status.TryRemoveStatusEffect(ev.User, ev.EffectProto);
    }

    private void OnRemove(Entity<CrucibleSoulStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (ent.Comp.Coords == null || TerminatingOrDeleted(args.Target))
            return;

        _pull.StopAllPulls(args.Target);
        _transform.SetCoordinates(args.Target, ent.Comp.Coords.Value);
        _transform.AttachToGridOrMap(args.Target);
    }

    private void OnApply(Entity<CrucibleSoulStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        ent.Comp.Coords = Transform(args.Target).Coordinates;
    }
}
