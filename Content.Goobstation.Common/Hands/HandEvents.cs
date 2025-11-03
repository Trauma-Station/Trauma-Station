using System.Numerics;

namespace Content.Goobstation.Common.Hands;

/// <summary>
///     Raised directed on both the blocking entity and user when
///     a virtual hand item is thrown (at least attempted to).
/// </summary>
[ByRefEvent]
public readonly record struct VirtualItemThrownEvent(EntityUid BlockingEntity, EntityUid User, EntityUid VirtualItem, Vector2 Direction);

/// <summary>
///     Raised directed on both the blocking entity and user when
///     user tries to drop it by keybind.
///     Cancellable.
/// </summary>
[ByRefEvent]
public record struct VirtualItemDropAttemptEvent(EntityUid BlockingEntity, EntityUid User, EntityUid VirtualItem, bool Throw, bool Cancelled = false);
