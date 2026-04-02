// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;

namespace Content.Trauma.Shared.ClockworkCult.Scripture;

/// <summary>
/// This handles charges related events for reciting scriptures
/// </summary>
public sealed partial class ScriptureSystem
{
    [Dependency] private readonly SharedChargesSystem _charges = default!;

    private void InitializeCharges()
    {
        SubscribeLocalEvent<LimitedChargesComponent, ReciteAttemptEvent>(OnRecite);
    }

    private void OnRecite(Entity<LimitedChargesComponent> ent, ref ReciteAttemptEvent args)
    {
        if (ent.Comp.LastCharges >= args.ScriptureCost)
        {
            _charges.TryUseCharges(ent.AsNullable(), args.ScriptureCost);
            return;
        }

        Log.Debug("Not Enough Power!");
        args.Cancelled = true;
    }
}
