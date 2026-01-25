// SPDX-License-Identifier: AGPL-3.0-or-later
namespace Content.Trauma.Common.Paper;

/// <summary>
/// Raised on a paper entity after it has been signed.
/// Set <see cref="Handled"/> to true to prevent popups.
/// </summary>
[ByRefEvent]
public record struct PaperSignedEvent(EntityUid User, bool Handled = false);
