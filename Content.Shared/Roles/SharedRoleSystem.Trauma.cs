using Robust.Shared.Network;

namespace Content.Shared.Roles;

public abstract partial class SharedRoleSystem
{
    [Dependency] private INetManager _net = default!;
}
