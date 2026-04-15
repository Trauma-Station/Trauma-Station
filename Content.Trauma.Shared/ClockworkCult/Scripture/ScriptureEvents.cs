// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.ClockworkCult.Scripture;

/// <summary>
/// Raised when the user presses the Recite button in the UI of the Clockwork Slab
/// </summary>
[Serializable, NetSerializable]
public sealed class ScriptureReciteMessage(EntProtoId scripture, string? tierData) : BoundUserInterfaceMessage
{
    /// <summary>
    ///  The scripture we want to recite.
    /// </summary>
    public EntProtoId Scripture = scripture;

    /// <summary>
    /// Current selected tier, if it exists.
    /// </summary>
    public string? TierData = tierData;
}

/// <summary>
/// Raised on the user to check if the recite can succeed (do we have enough power to cast?).
/// </summary>
[ByRefEvent]
public record struct ReciteAttemptEvent(int ScriptureCost, bool Cancelled = false);
