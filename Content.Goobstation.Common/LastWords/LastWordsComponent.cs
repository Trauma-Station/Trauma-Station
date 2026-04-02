// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Common.LastWords;

/// <summary>
/// Tracks the last words a user has said.
/// </summary>
[RegisterComponent]
public sealed partial class LastWordsComponent : Component
{
    [DataField]
    public string? LastWords;
}
