// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Surgery.Tools;
using Content.Shared.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Medical.Shared.Surgery;

/// <summary>
///     Raised on the step entity and the user after doing a step. Piece of shit.
/// </summary>
[ByRefEvent]
public record struct SurgeryStepEvent(EntityUid User, EntityUid Body, EntityUid Part, EntityUid Tool, ProtoId<SurgeryPrototype> SurgeryId, bool Complete);

/// <summary>
/// Raised on the user after failing to do a step for any reason. Piece of shit.
/// </summary>
[ByRefEvent]
public record struct SurgeryStepFailedEvent(EntityUid User, EntityUid Body, ProtoId<SurgeryPrototype> SurgeryId);

/// <summary>
/// Checks if surgery can be performed. Piece of shit.
/// </summary>
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

/// <summary>
/// Checks if the surgery can be completed.
/// </summary>
[ByRefEvent]
public record struct SurgeryStepCompleteCheckEvent(EntityUid Body, EntityUid Part, EntityUid Surgery, bool Cancelled = false);
