// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.Areas;

/// <summary>
/// Component that runs entity effects when you enter/exit an area.
///
/// Requires <see cref="AreaDetectorComponent"/> to work.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EffectsOnAreaDetectComponent : Component
{
    /// <summary>
    /// The areas to look out for.
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId> Areas = new();

    /// <summary>
    /// Effects to run when entering the area.
    /// </summary>
    [DataField]
    public EntityEffect[]? EffectsOnEnter;

    /// <summary>
    /// Effects to run when exiting the area.
    /// </summary>
    [DataField]
    public EntityEffect[]? EffectsOnExit;
}
