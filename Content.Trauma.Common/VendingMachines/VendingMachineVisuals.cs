// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.VendingMachines; // Trauma - moved out of Content.Client and made them serializable...

[Serializable, NetSerializable]
public enum VendingMachineVisualState : byte
{
    Normal,
    Off,
    Broken,
    Eject,
    Deny
}

[Serializable, NetSerializable]
public enum VendingMachineVisualLayers : byte
{
    /// <summary>
    /// Off / Broken. The other layers will overlay this if the machine is on.
    /// </summary>
    Base,

    /// <summary>
    /// Normal / Deny / Eject
    /// </summary>
    BaseUnshaded,

    /// <summary>
    /// Screens that are persistent (where the machine is not off or broken)
    /// </summary>
    Screen
}
