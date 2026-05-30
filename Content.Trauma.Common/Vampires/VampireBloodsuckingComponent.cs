// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Vampires;

/// <summary>
/// Component that allows user to drain blood from a valid entity by attacking them in combat mode,
/// whilst the head of the target is targeted.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class VampireBloodsuckingComponent : Component
{
    /// <summary>
    ///  In all cases, this is how much hunger we restore, in case a <see cref="BloodSuckDoAfterEvent"/> succeeds.
    /// </summary>
    [DataField]
    public float HungerRestoration = 100f;

    /// <summary>
    ///  How much blood will we remove from the target?
    /// </summary>
    [DataField]
    public int BloodToRemove = 25;

    /// <summary>
    /// A hashset of consumed victims.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> ConsumedVictims = new();

    /// <summary>
    /// How long the bloodsucking DoAfter lasts for.
    /// </summary>
    [DataField]
    public TimeSpan BloodsuckingDelay = TimeSpan.FromSeconds(5f);
}
