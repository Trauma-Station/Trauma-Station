// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Actions;

/// <summary>
/// Action component that removes itself after being performed.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SelfRemovingActionComponent : Component;
