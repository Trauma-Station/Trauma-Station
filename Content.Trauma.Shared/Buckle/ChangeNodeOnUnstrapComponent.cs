// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Buckle;

/// <summary>
/// Changes the entity's construction node when a mob is unstrapped from it.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(StrapLockSystem))]
public sealed partial class ChangeNodeOnUnstrapComponent : Component
{
    [DataField(required: true)]
    public string Node = string.Empty;
}
