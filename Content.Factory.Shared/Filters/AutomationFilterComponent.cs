// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Factory.Shared.Filters;

/// <summary>
/// Marker component for filter items.
/// Only used for whitelisting, does nothing on its own.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AutomationFilterComponent : Component;

/// <summary>
/// Event raised on a filter to determine if it should block an item.
/// If <c>CouldAllow</c> is set to true, IsAlwaysBlocked will return false.
/// </summary>
[ByRefEvent]
public record struct AutomationFilterEvent(EntityUid Item, bool Allowed = false, bool CouldAllow = false);

/// <summary>
/// Event raised on a filter to get its stack split size.
/// </summary>
[ByRefEvent]
public record struct AutomationFilterSplitEvent(int Size = 0);

/// <summary>
/// UI key all complex filters can use for configuring them.
/// </summary>
[Serializable, NetSerializable]
public enum FilterUiKey : byte
{
    Key
}
