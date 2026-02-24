// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Common.Surgery.Tools;
using Content.Shared.Inventory;

namespace Content.Medical.Shared.Surgery.Steps;

[ByRefEvent]
public record struct SurgeryCanPerformStepEvent(
    EntityUid User,
    EntityUid Body,
    EntityUid Tool,
    SlotFlags TargetSlots,
    string? Popup = null,
    StepInvalidReason Invalid = StepInvalidReason.None,
    ISurgeryToolComponent? ValidTool = null
) : IInventoryRelayEvent
{
    public bool IsValid => Invalid == StepInvalidReason.None;
    public bool IsInvalid => !IsValid;
}
