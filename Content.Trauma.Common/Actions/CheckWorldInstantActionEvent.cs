// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Actions;

/// <summary>
/// Checks to see if a world target action can fallback if its validation failed.
/// </summary>
[ByRefEvent]
public record struct CheckWorldInstantActionEvent(EntityUid User, EntityUid Provider, bool Handled = false);
