// SPDX-License-Identifier: AGPL-3.0-or-later
namespace Content.Trauma.Common.Papers;

/// <summary>
/// Raised on a paper entity after its content is changed.
/// </summary>
[ByRefEvent]
public readonly record struct PaperWrittenEvent(EntityUid? User, string Content);
