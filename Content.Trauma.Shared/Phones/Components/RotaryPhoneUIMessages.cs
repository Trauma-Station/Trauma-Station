// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.Phones.Components;

[Serializable, NetSerializable]
public sealed class PhoneKeypadMessage(int value) : BoundUserInterfaceMessage
{
    public readonly int Value = value;
}

[Serializable, NetSerializable]
public sealed class PhoneBookPressedMessage(int value) : BoundUserInterfaceMessage
{
    public readonly int Value = value;
}

[Serializable, NetSerializable]
public sealed class PhoneNameChangedMessage(string value) : BoundUserInterfaceMessage
{
    public readonly string Value = value;
}

[Serializable, NetSerializable]
public sealed class PhoneCategoryChangedMessage(string value) : BoundUserInterfaceMessage
{
    public readonly string Value = value;
}

[Serializable, NetSerializable]
public sealed class PhoneKeypadClearMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class PhoneDialedMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class GoobPhoneBuiState(List<PhoneData> phones) : BoundUserInterfaceState
{
    public readonly List<PhoneData> Phones = phones;
}

[Serializable, NetSerializable]
public record struct PhoneData(string Name, ProtoId<PhoneCategoryPrototype> Category, int Number);
