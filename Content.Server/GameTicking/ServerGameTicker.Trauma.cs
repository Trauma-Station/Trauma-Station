// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Systems;

namespace Content.Server.GameTicking;

public sealed partial class ServerGameTicker
{
    [Dependency] private DamageableSystem _damageable = default!;

    public override int ReadyPlayerCountEffective()
    {
        int total = ReadyPlayerCount();
        return total + (_playerGameStatuses.Count - total) / 2;
    }
}
