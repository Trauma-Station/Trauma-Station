// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Antag;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.Antag;

public sealed partial class AntagPlayerEffectsSystem : EntitySystem
{
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    [SubscribeLocalEvent]
    private void OnEntitySelected(Entity<AntagPlayerEffectsComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        _effects.ApplyEffects(args.EntityUid, ent.Comp.Effects, predicted: false); // probably not predicted yet
    }
}
