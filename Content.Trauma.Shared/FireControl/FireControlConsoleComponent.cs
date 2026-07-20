// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.FireControl;

/// <summary>
/// These are for the consoles that provide the user interface for fire control servers.
/// </summary>
[RegisterComponent]
public sealed partial class FireControlConsoleComponent : Component
{
    [ViewVariables]
    public EntityUid? ConnectedServer = null;
}
