// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared.Revolutionary.Components;

/// <summary>
/// Trauma - headrev conversion field
/// </summary>
public sealed partial class HeadRevolutionaryComponent
{
    /// <summary>
    /// If head rev's convert ability is not disabled by mindshield
    /// </summary>
    [DataField]
    public bool ConvertAbilityEnabled = true;
}
