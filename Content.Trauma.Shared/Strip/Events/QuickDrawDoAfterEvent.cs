// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;

namespace Content.Trauma.Shared.Strip.Events;

[Serializable, NetSerializable]
public sealed partial class QuickDrawDoAfterEvent : DoAfterEvent
{
    public readonly NetEntity SlotEntity;
    public readonly string SlotId;
    public readonly bool Stealth;

    public QuickDrawDoAfterEvent(NetEntity slotEntity, string slotId, bool stealth)
    {
        SlotEntity = slotEntity;
        SlotId = slotId;
        Stealth = stealth;
    }

    public override DoAfterEvent Clone() => new QuickDrawDoAfterEvent(SlotEntity, SlotId, Stealth);
}
