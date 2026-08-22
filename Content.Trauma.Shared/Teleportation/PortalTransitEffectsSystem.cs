// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Trauma.Common.Teleportation;

namespace Content.Trauma.Shared.Teleportation;

public sealed partial class PortalTransitEffectsSystem : EntitySystem
{
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    [SubscribeLocalEvent]
    private void OnPortalTeleported(Entity<PortalTransitEffectsComponent> ent, ref PortalTeleportedEvent args)
    {
        if (ent.Comp.Effects is { } effects)
            _effects.ApplyEffects(ent, effects);
        if (ent.Comp.SourceEffects is { } srcEffects)
            _effects.ApplyEffects(args.Source, srcEffects);
        if (ent.Comp.DestEffects is { } destEffects && args.Dest is { } dest)
            _effects.ApplyEffects(dest, destEffects);
    }
}
