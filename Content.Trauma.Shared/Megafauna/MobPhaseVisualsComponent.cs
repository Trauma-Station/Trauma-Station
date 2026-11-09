// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Megafauna;

/// <summary>
/// Component that changes the visuals of the megafauna based on the current phase
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MobPhaseVisualsComponent : Component;


[Serializable, NetSerializable]
public enum MobPhaseVisuals : byte
{
    Key,
    Layer,
}
