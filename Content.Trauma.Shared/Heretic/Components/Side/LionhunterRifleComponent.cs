// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Heretic.Components.Side;

/// <summary>
/// Empowers projectiles if aimed at the target, controlled by <see cref="AimedRifleComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class LionhunterRifleComponent : Component;
