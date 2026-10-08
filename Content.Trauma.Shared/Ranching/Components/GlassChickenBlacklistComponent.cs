// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Ranching.Components;

/// <summary>
/// Prevents a food from being duped when eaten by a glass chicken.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GlassChickenBlacklistComponent : Component;
