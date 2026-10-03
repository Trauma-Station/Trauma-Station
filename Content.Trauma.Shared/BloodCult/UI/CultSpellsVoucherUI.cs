// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.BloodCult.UI;

/// <summary>
/// Message for a mining voucher kit to be selected.
/// </summary>
[Serializable, NetSerializable]
public sealed class CultSpellsSelectMessage(int index) : BoundUserInterfaceMessage
{
    public readonly int Index = index;
}

[Serializable, NetSerializable]
public enum CultSpellsUiKey : byte
{
    Key
}
