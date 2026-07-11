// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Nuclear.Reactor;

[Serializable, NetSerializable]
public enum NuclearReactorUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class NuclearReactorBuiState(ReactorSlotBUIData[] data) : BoundUserInterfaceState
{
    public readonly ReactorSlotBUIData[] SlotData = data;
}

[Serializable, NetSerializable]
public sealed class ReactorSlotBUIData
{
    public double Temperature = 0;
    public int NeutronCount = 0;

    public float NeutronRadioactivity = 0f;
    public float Radioactivity = 0f;
    public float SpentFuel = 0f;
}

/// <summary>
/// Message to swap a reactor part at a position with the reactor' part itemslot.
/// </summary>
[Serializable, NetSerializable]
public sealed class ReactorSwapPartMessage(Vector2i position) : BoundUserInterfaceMessage
{
    public Vector2i Position { get; } = position;
}

/// <summary>
/// Message to eject the reactor's part itemslot.
/// </summary>
[Serializable, NetSerializable]
public sealed class ReactorEjectItemMessage : BoundUserInterfaceMessage;

/// <summary>
/// Message to change the control rods insertion target by adding/subtracing a value to it.
/// </summary>
[Serializable, NetSerializable]
public sealed class ReactorAdjustControlRodsMessage(float change) : NuclearMachineBUIMessage
{
    public float Change { get; } = change;
}
