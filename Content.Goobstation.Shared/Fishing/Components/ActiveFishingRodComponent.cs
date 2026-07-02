// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Goobstation.Shared.Fishing.Components;

/// <summary>
/// Component added to fishing rod while the lure is attached to a fishing spot.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ActiveFishingRodComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Fisher;

    [DataField, AutoNetworkedField]
    public TimeSpan? FishingStartTime;

    /// <summary>
    /// If true, someone is pulling fish out of this spot.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Reeling;

    [DataField, AutoNetworkedField]
    public float FishDifficulty;

    /// <summary>
    /// Fish that we're currently trying to catch
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId? Fish;
}
