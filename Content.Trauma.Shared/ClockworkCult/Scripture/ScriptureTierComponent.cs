// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.ClockworkCult.Scripture;

/// <summary>
/// TODO: Implement it in ui and playtest it
///
/// Enables the use of tiers on a scripture.
///
/// A tier is just an upgraded version of a scripture,
/// usually it provides benefits or unique interactions to the scripture.
///
/// All it does it replace the default Recital Effects of the scripture with the corresponding's tier effects.
///
/// Each tier must have an unlock condition.
/// Tier 1 is always the default effects.
/// A scripture can have as many tiers as you want, but it's recommended to have a unique maximum of 3, with 2 being the standard.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ScriptureTierComponent : Component
{
    /// <summary>
    /// The tiers this scripture has
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ScriptureTierData> Tiers;
}

/// <summary>
/// A basic data structure holding scripture tiers.
/// </summary>
[DataRecord, NetSerializable, Serializable]
public partial record struct ScriptureTierData
{
    /// <summary>
    /// Unique identifier for the data
    /// </summary>
    [DataField(required: true)]
    public string Id;

    /// <summary>
    /// Explain what this tier adds to this scripture.
    ///
    /// Will appear as a tooltip in the ui.
    /// </summary>
    [DataField]
    public string Description;

    // TODO: Add unlock condition

    /// <summary>
    /// The new effects this tier will replace the default ones with.
    /// </summary>
    [DataField]
    public EntityEffect[] RecitalEffects;

    /// <summary>
    /// Is this tier locked by default? You'll want to leave this true for everything except T1.
    /// </summary>
    [DataField]
    public bool Locked;
}
