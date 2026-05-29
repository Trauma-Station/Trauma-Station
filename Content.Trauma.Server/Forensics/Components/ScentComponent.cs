// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Server.Forensics.Components;

/// <summary>
/// This component is for mobs that have a Scent.
/// </summary>
[RegisterComponent]
public sealed partial class ScentComponent : Component
{
    [DataField]
    public string Scent = string.Empty;
}
