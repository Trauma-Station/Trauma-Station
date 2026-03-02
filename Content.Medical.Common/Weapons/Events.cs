// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Medical.Common.Weapons;

[ByRefEvent]
public record struct AttemptHandsMeleeEvent(bool Cancelled = false);

[ByRefEvent]
public record struct AttemptHandsShootEvent(bool Cancelled = false);
