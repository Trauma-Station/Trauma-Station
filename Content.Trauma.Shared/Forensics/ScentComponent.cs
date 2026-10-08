// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Forensics;

/// <summary>
/// This component is for mobs that have a Scent.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ScentComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Scent = string.Empty;
}
