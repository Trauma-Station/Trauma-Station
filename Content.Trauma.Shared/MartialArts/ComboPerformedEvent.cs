// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.MartialArts;

/// <summary>
/// Event raised on a martial art entity when a combo has been performed.
/// </summary>
[ByRefEvent]
public record struct ComboPerformedEvent(EntityUid User, EntityUid Target);
