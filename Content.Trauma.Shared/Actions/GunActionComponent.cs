// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Actions;

/// <summary>
/// Component for a world target action to shoot the action's gun where you used the action.
/// The action must raise the event on itself and have GunComponent, etc.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GunActionComponent : Component;
