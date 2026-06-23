using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Orion.Bitrunning;

public sealed partial class BitrunningDisconnectAvatarActionEvent : InstantActionEvent;

[Serializable, NetSerializable]
public sealed partial class BitrunningDisconnectAvatarDoAfterEvent : SimpleDoAfterEvent;

/// <summary>
/// Raised on an entity to check if it should be excluded from antag selection.
/// </summary>
[ByRefEvent]
public record struct GetAntagSelectionBlockerEvent(bool Blocked = false);
