// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.StatusEffectNew;
using Content.Trauma.Shared.Heretic.Components.StatusEffects;
using Content.Trauma.Shared.Heretic.Events;
using Content.Trauma.Shared.Teleportation;

namespace Content.Trauma.Shared.Heretic.Crucible.Systems;

public sealed class CrucibleSoulStatusEffectSystem : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly TeleportSystem _teleport = default!;

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
        if (ent.Comp.Coords is not { } coords || TerminatingOrDeleted(args.Target))
            return;

        _teleport.Teleport(args.Target, coords);
    }

    private void OnApply(Entity<CrucibleSoulStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        ent.Comp.Coords = Transform(args.Target).Coordinates;
    }
}
