// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Vampires.Gargantua;

/// <summary>
/// A component applied to a projectile to do special behavior listed below.
///
/// If in combat mode, the target will be pulled towards you once it collides with them.
/// If in non-combat mode, the target will be pushed away from you once it collides with them.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DemonicHandComponent : Component;
