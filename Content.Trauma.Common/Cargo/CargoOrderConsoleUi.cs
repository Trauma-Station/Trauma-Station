// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Common.Cargo;

/// <summary>
/// Message to set a cargo request console's destination telepad.
/// </summary>
[Serializable, NetSerializable]
public sealed class CargoConsoleSetDestinationMessage(NetEntity? dest) : BoundUserInterfaceMessage
{
    public readonly NetEntity? Destination = dest;
}
