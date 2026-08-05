// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.DoAfter;

namespace Content.Trauma.Shared.ClockworkCult;

public sealed partial class EventClockworkConvert : EntityTargetActionEvent;

[Serializable, NetSerializable]
public sealed partial class ClockworkConvertDoAfterEvent : SimpleDoAfterEvent;
