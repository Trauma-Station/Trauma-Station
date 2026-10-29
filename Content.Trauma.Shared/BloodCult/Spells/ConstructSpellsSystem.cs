// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;

namespace Content.Trauma.Shared.BloodCult.Spells;

public sealed partial class ConstructSpellsSystem : EntitySystem
{
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnArtificerHeal(BloodCultArtificerHealEvent args)
    {
        if (args.Handled)
            return;

        var ev = new RejuvenateEvent(false, false);
        RaiseLocalEvent(args.Target, ev);

        if (_mind.TryGetMind(args.Target, out var mindId, out var mind))
            _mind.UnVisit(mindId, mind);

        _popup.PopupEntity($"You restore this construct to its intended form.", args.Performer, args.Performer);

        args.Handled = true;
    }
}
