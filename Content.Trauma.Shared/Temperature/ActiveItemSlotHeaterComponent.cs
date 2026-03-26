// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Temperature;

/// <summary>
/// Applied automatically when an entity gets inserted into an item slot,
/// so the update loop doesn't run when there's no items to heat.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActiveItemSlotHeaterComponent : Component;
