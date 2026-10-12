// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Mind;
using Content.Shared.Rejuvenate;
using Content.Trauma.Shared.BloodCult.Constructs;

namespace Content.Trauma.Shared.BloodCult.Spells;

public sealed partial class ConstructSpellsSystem : EntitySystem
{
    [Dependency] private SharedMindSystem _mind = default!;

    [SubscribeLocalEvent]
    private void OnArtificerHeal(Entity<ConstructComponent> ent, ref BloodCultArtificerHealEvent args)
    {
        var ev = new RejuvenateEvent(false, false);
        RaiseLocalEvent(args.Target, ev);
        if (_mind.TryGetMind(args.Target, out var mindId, out var mind))
            _mind.UnVisit(mindId, mind);
    }
}
