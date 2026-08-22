// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;

namespace Content.Trauma.Shared.Strip.Events;

[Serializable, NetSerializable]
public sealed partial class BagAccessDoAfterEvent : DoAfterEvent
{
    public readonly string SlotName;
    public readonly NetEntity BagEntity;
    public readonly bool Stealth;

    public BagAccessDoAfterEvent(string slotName, NetEntity bagEntity, bool stealth)
    {
        SlotName = slotName;
        BagEntity = bagEntity;
        Stealth = stealth;
    }

    public override DoAfterEvent Clone() => new BagAccessDoAfterEvent(SlotName, BagEntity, Stealth);
}
