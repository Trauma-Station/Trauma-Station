// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.Heretic;

/// <summary>
///     Indicates that an entity can act as a protective blade.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ProtectiveBladeComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid User;
}
