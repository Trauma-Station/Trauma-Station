// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.BloodCult.UI;

[Serializable, NetSerializable]
public enum NameSelectorUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class NameSelectedMessage(string name) : BoundUserInterfaceMessage
{
    public readonly string Name = name;
}
