// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.Phones.Components;

[Serializable, NetSerializable]
public sealed class PhoneKeypadMessage(int value) : BoundUserInterfaceMessage
{
    public readonly int Value = value;
}

[Serializable, NetSerializable]
public sealed class PhoneBookPressedMessage : BoundUserInterfaceMessage
{
    public int Value;

    public PhoneBookPressedMessage(int value)
    {
        Value = value;
    }
}

[Serializable, NetSerializable]
public sealed class PhoneNameChangedMessage : BoundUserInterfaceMessage
{
    public string Value;

    public PhoneNameChangedMessage(string value)
    {
        Value = value;
    }
}

[Serializable, NetSerializable]
public sealed class PhoneCategoryChangedMessage : BoundUserInterfaceMessage
{
    public string Value;

    public PhoneCategoryChangedMessage(string value)
    {
        Value = value;
    }
}

[Serializable, NetSerializable]
public sealed class PhoneKeypadClearMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class PhoneDialedMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class GoobPhoneBuiState : BoundUserInterfaceState
{
    public List<PhoneData> Phones { get; }

    public GoobPhoneBuiState(List<PhoneData> phones)
    {
        Phones = phones;
    }
}

[Serializable, NetSerializable]
public record struct PhoneData
{
    public string Name;
    public string Category;
    public int Number;

    public PhoneData(string name, string category, int number)
    {
        Name = name;
        Category = category;
        Number = number;
    }
}
