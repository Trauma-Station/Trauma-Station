// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Mobs;

/// <summary>
/// Marker component added to post-init mobs that are currently in softcrit.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SoftCritMobComponent : Component;
