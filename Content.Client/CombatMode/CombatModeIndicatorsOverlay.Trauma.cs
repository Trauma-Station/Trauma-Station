using Robust.Client.GameObjects;
using Robust.Client.Player;

namespace Content.Client.CombatMode;

public sealed partial class CombatModeIndicatorsOverlay
{
    [Dependency] private IPlayerManager _player = default!;
    private readonly SpriteSystem _sprite;
}
