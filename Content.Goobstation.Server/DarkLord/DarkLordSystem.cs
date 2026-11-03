// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.DarkLord;
using Content.Shared.GameTicking;
using Robust.Shared.Random;

namespace Content.Goobstation.Server.DarkLord;

public sealed partial class DarkLordSystem : EntitySystem
{
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private IRobustRandom _random = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<DarkLordComponent> ent, ref MapInitEvent args)
    {
        if (_random.Prob(ent.Comp.ChosenOneChance))
            _ticker.StartGameRule(ent.Comp.Rule);
    }
}
