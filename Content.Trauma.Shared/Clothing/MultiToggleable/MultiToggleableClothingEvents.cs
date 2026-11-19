// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;

namespace Content.Trauma.Shared.Clothing.MultiToggleable;

/// <summary>
/// Raised on toggleable clothing entity before it is toggled
/// Can be used to cancel the toggle process, for example, when seals are active
/// <paramref name="Multiple"/> is set to true if multiple attached clothing are being toggled at once
/// </summary>
[ByRefEvent]
public record struct ToggleClothingAttemptEvent(EntityUid Wearer, EntityUid User, EntityUid Clothing, bool Multiple, bool Cancelled = false);

[Serializable, NetSerializable]
public sealed partial class AttachClothingDoAfterEvent : SimpleDoAfterEvent;
