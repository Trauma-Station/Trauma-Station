// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.PullBlocker;

/// <summary>
/// This stops entities from pulling the entity this component is attached to
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BlockPullComponent : Component;
