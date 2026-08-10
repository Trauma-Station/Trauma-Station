// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Electrocution;
using Content.Shared.Interaction.Events;
using Content.Shared.Xenoborgs.Components;

namespace Content.Trauma.Shared.BlockShuttleRecall;

/// <summary>
/// Blocks shuttle recalls from things like abductors.
/// </summary>
public sealed partial class BlockShuttleRecallSystem : EntitySystem
{
    [Dependency] private SharedElectrocutionSystem _electrocution = default!;

    [SubscribeLocalEvent]
    private void OnInteractAttempt(Entity<BlockShuttleRecallComponent> ent, ref InteractionAttemptEvent args)
    {
        if (!HasComp<TraumaCommsConsoleComponent>(args.Target))
            return;

        args.Cancelled = true;
        _electrocution.TryDoElectrocution(ent.Owner, null, ent.Comp.ShockDamage, ent.Comp.ShockTime, true, ignoreInsulation: true);
    }
}
