// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.Bitrunning;

[Serializable, NetSerializable]
public enum BitrunningDiskUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class BitrunningDiskBoundUiState(List<string> options, string? selectedOption) : BoundUserInterfaceState
{
    public List<string> Options = options;
    public string? SelectedOption = selectedOption;
}

[Serializable, NetSerializable]
public sealed class BitrunningDiskSelectOptionMessage(string option) : BoundUserInterfaceMessage
{
    public string Option = option;
}
