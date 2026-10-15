// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Illusion;

/// <summary>
/// Spawns illusion when attack/projectile is parried using item with this component
/// Requires <see cref="IllusionSpawnerComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class IllusionOnParryComponent : Component;
