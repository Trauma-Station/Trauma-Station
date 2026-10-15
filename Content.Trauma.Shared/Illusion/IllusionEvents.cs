// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Illusion;

/// <summary>
/// Raised on cloned held item when it's copied over when creating illusion of someone
/// </summary>
[ByRefEvent]
public readonly record struct IllusionHeldItemCopiedEvent(EntityUid OriginalItem, EntityUid ClonedItem);
