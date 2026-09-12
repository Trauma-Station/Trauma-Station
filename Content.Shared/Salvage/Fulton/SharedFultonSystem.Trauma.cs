using Robust.Shared.Network;

namespace Content.Shared.Salvage.Fulton;

public abstract partial class SharedFultonSystem
{
    [Dependency] private INetManager _net = default!;
}
