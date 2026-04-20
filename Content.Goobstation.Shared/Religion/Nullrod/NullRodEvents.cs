// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Religion.Nullrod.Components;
using Content.Shared.DoAfter;

namespace Content.Goobstation.Shared.Religion.Nullrod;

/// <summary>
/// 	Raised on the nullrod when praying.
/// </summary>
/// <param name="User">The entity praying at the nullrod.</param>
[ByRefEvent]
public record struct AlternatePrayEvent(EntityUid User);

[Serializable, NetSerializable]
public sealed partial class AlternatePrayDoAfterEvent : SimpleDoAfterEvent;
