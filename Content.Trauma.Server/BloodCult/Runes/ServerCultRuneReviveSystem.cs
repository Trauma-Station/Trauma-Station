// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Ghost;
using Content.Server.EUI;
using Content.Shared.Mind;
using Content.Trauma.Shared.BloodCult.Runes.Revive;
using Robust.Shared.Player;

namespace Content.Trauma.Server.BloodCult.Runes;

public sealed partial class ServerCultRuneReviveSystem : CultRuneReviveSystem
{
    [Dependency] private EuiManager _eui = default!;

    protected override void OpenReturnEui(Entity<MindComponent> mind, ICommonSession session)
    {
        // mfw like 5 different things have to keep doing this slop :)
        _eui.OpenEui(new ReturnToBodyEui(mind, Mind, Player), session);
    }
}
