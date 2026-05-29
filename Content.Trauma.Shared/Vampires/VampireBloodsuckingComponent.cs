// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;

namespace Content.Trauma.Shared.Vampires;

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

/// <summary>
/// Raised on the <see cref="VampireBloodsuckingComponent"/> entity, after the bloodsucking process starts.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class BloodSuckDoAfterEvent : SimpleDoAfterEvent;

/// <summary>
/// Raised on the entity that does the bloodsucking sequence, and it passes.
/// </summary>
/// <param name="BloodRemoved"></param>The blood that was removed from the target during the bloodsucking sequence.
[ByRefEvent]
public record struct BloodsuckingSuccessEvent(int BloodRemoved);

/// <summary>
/// Raised on the target to validate whether they can be drained of their blood.
/// </summary>
[ByRefEvent]
public record struct BloodsuckingAttemptEvent(bool Cancelled = false);
