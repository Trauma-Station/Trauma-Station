// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.Teleportation;

/// <summary>
/// Applies entity effects after going through a portal.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PortalTransitEffectsComponent : Component
{
    /// <summary>
    /// Effects applied to this entity.
    /// </summary>
    [DataField]
    public EntityEffect[]? Effects;

    /// <summary>
    /// Effects applied to the portal it entered through.
    /// </summary>
    [DataField]
    public EntityEffect[]? SourceEffects;

    /// <summary>
    /// Effects applied to the portal it left from.
    /// </summary>
    [DataField]
    public EntityEffect[]? DestEffects;
}
