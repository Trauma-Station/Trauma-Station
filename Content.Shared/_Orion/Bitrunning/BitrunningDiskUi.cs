using Robust.Shared.Serialization;

namespace Content.Shared._Orion.Bitrunning;

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
