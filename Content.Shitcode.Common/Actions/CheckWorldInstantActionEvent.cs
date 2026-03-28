// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Shitcode.Common.Actions;


/// <summary>
/// Checks to see if an action can fallback.
/// </summary>
[Serializable, NetSerializable]
public record struct CheckWorldInstantActionEvent(EntityUid User, EntityUid Provider, bool Fallback = false);
