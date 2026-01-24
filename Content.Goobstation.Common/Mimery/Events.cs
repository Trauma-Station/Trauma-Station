namespace Content.Goobstation.Common.Mimery;

/// <summary>
/// Raised on action when it gets added.
/// </summary>
/// <param name="Container">Action container</param>
[ByRefEvent]
public readonly record struct ActionGotAddedEvent(EntityUid Container);
