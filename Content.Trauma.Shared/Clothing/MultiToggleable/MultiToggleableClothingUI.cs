// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Clothing.MultiToggleable;

[Serializable, NetSerializable]
public enum MultiToggleClothingUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class MultiToggleableClothingUiMessage(NetEntity attachedClothingUid) : BoundUserInterfaceMessage
{
    public NetEntity AttachedClothingUid = attachedClothingUid;
}
