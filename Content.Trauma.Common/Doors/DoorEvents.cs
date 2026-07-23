// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Doors;

[ByRefEvent]
public record struct ShouldDoorCrushEvent(bool ShouldCrush, TimeSpan CrushDelay);

[ByRefEvent]
public record struct DoorOpenedEvent(EntityUid Door, EntityUid? User);
