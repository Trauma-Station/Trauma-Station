using Robust.Shared.Player;

namespace Content.Shared.Nutrition.EntitySystems;

public sealed partial class ThirstSystem
{
    [Dependency] private ISharedPlayerManager _player = default!;
}
