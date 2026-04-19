// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Actions;

/// <summary>
/// Action component that removes an action when it is used.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RemoveActionOnPerformComponent : Component;
