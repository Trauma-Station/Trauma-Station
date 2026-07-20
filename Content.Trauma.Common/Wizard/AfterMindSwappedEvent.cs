// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Wizard;

[ByRefEvent]
public record struct AfterMindSwappedEvent(EntityUid Performer, EntityUid Target);
