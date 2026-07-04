using Content.Shared.Strip;

namespace Content.Shared.Hands.EntitySystems;

public abstract partial class SharedHandsSystem
{
    [Dependency] private ThievingSystem _thieving = default!;
}
