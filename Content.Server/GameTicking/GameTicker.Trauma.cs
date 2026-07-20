// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Systems;

namespace Content.Server.GameTicking;

public sealed partial class GameTicker
{
    [Dependency] private DamageableSystem _damageable = default!;
}
