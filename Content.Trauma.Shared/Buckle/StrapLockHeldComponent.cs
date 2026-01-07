// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Buckle;

/// <summary>
/// Component given to someone being held up on a <see cref="StrapLockComponent"/>.
/// Used to remove <see cref="StrapLockHoldingComponent"/> from the holder when the strap gets locked.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(StrapLockSystem))]
[AutoGenerateComponentState]
public sealed partial class StrapLockHeldComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Holder;

    [DataField]
    public bool Unsafe = true;
}
