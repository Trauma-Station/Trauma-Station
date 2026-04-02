// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.Examine;

/// <summary>
/// Gives an <c>ItemToggleComponent</c> an examine message depending on it being enabled or disabled.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ToggleExamineSystem))]
public sealed partial class ToggleExamineComponent : Component
{
    [DataField(required: true)]
    public LocId Enabled;

    [DataField(required: true)]
    public LocId Disabled;
}
