// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Strip.Components;

/// <summary>
/// Added to outer clothing,
/// ignores blocking of removing target's inner clothing when equipped
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StripBlockBypassComponent : Component;
