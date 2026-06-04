// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Vampires.Lair;

/// <summary>
/// Used for making the vampiric rune get the color of its creator's blood.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class VampiricRuneVisualsComponent : Component;

[Serializable, NetSerializable]
public enum VampiricRuneVisuals : byte
{
    Color,
}
