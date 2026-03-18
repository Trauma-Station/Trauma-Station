using Content.Shared.Damage.Systems;

namespace Content.Server.GameTicking;

public sealed partial class GameTicker
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
}
