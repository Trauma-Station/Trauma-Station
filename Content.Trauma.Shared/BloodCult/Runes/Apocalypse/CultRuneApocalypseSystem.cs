// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;
using Robust.Shared.Random;

namespace Content.Trauma.Shared.BloodCult.Runes.Apocalypse;

public abstract partial class CultRuneApocalypseSystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfter = default!;

    [SubscribeLocalEvent]
    private void OnRuneInvoke(Entity<CultRuneApocalypseComponent> ent, ref RuneInvokeEvent args)
    {
        if (ent.Comp.Used)
            return;

        var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.InvokeTime, new ApocalypseRuneDoAfter(), ent)
        {
            BreakOnMove = true
        };

        args.Handled = _doAfter.TryStartDoAfter(doAfter);
    }
}

[Serializable, NetSerializable]
public sealed partial class ApocalypseRuneDoAfter : SimpleDoAfterEvent;
