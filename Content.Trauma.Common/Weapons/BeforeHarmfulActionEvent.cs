// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Inventory;

namespace Content.Trauma.Common.Weapons;

[ByRefEvent]
public record struct BeforeHarmfulActionEvent(
    EntityUid User,
    EntityUid Target,
    HarmfulActionType Type,
    EntityUid? Used = null,
    bool CanRiposte = true,
    bool Cancelled = false)
    : IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.WITHOUT_POCKET;
}

public enum HarmfulActionType : byte
{
    Harm,
    Disarm,
    Grab,
}
