// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.StatusIcon;

namespace Content.Trauma.Shared.CosmicCult.Components.Examine;

/// <summary>
/// Marker component for targets under the effect of Shunt Subjectivity or Astral Projection.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CosmicBlankComponent : Component
{
    /// <summary>
    /// The status icon corresponding to the effect.
    /// </summary>
    [DataField]
    public ProtoId<SsdIconPrototype> StatusIcon = "CosmicSSDIcon";
}
