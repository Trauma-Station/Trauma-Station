// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.DoAfter;

namespace Content.Trauma.Shared.Clothing.Sealable;

[Serializable, NetSerializable]
public sealed partial class SealClothingDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class StartSealingProcessDoAfterEvent : SimpleDoAfterEvent;

public sealed partial class SealClothingEvent : InstantActionEvent;

/// <summary>
///     Raises on control when clothing finishes it's sealing or unsealing process
/// </summary>
[ByRefEvent]
public readonly record struct ClothingControlSealCompleteEvent(bool IsSealed);

/// <summary>
///     Raises on part when clothing finishes it's sealing or unsealing process
/// </summary>
[ByRefEvent]
public readonly record struct ClothingPartSealCompleteEvent(bool IsSealed);

[ByRefEvent]
public record struct ClothingSealAttemptEvent(EntityUid Wearer, EntityUid User, bool Cancelled = false);
