// SPDX-License-Identifier: AGPL-3.0-or-later
namespace Content.Trauma.Client.Projectiles;

/// <summary>
/// Added to make sure a projectile never has its light enabled.
/// </summary>
[RegisterComponent]
public sealed partial class HiddenProjectileComponent : Component;
