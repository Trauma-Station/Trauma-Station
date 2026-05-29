// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Wizard.Projectiles;

/// <summary>
/// Add this and TrailComponent to an entity so that it spawns a trail of that entity sprite.
/// TrailComponent's ParticleAmount should be set to zero for it to work correctly.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EntityTrailComponent : Component
{
}
