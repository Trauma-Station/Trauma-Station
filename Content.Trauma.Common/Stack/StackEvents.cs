// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Stack;

/// <summary>
/// Raised on the donor or recipient stack and determines if the qualities are similiar enough to merge
/// </summary>
/// <param name="Recipient"></param>
/// <param name="Cancelled"></param>
[ByRefEvent]
public record struct AttemptMergeStackEvent(EntityUid OtherStack, bool Cancelled);
