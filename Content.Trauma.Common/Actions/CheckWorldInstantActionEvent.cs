// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Actions;


/// <summary>
/// Checks to see if an action can fallback.
/// </summary>
[ByRefEvent]
public record struct CheckWorldInstantActionEvent(EntityUid User, EntityUid Provider, bool Fallback = false);
