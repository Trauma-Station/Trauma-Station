// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Common.Weapons.AmmoSelector;

[Serializable, NetSerializable]
public sealed class AmmoSelectedMessage(ProtoId<SelectableAmmoPrototype> protoId) : BoundUserInterfaceMessage
{
    public ProtoId<SelectableAmmoPrototype> ProtoId { get; } = protoId;
}

[Serializable, NetSerializable]
public enum AmmoSelectorUiKey : byte
{
    Key
}
