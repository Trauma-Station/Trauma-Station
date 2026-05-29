using Robust.Shared.Prototypes; // Trauma
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(ZombieRuleSystem))]
public sealed partial class ZombieRuleComponent : Component
{
    /// <summary>
    /// When the round will next check for round end.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan? NextRoundEndCheck;

    /// <summary>
    /// The amount of time between each check for the end of the round.
    /// </summary>
    [DataField]
    public TimeSpan EndCheckDelay = TimeSpan.FromSeconds(30);

    /// <summary>
    /// After this amount of the crew become zombies, the shuttle will be automatically called.
    /// </summary>
    [DataField]
    public float ZombieShuttleCallPercentage = 0.7f;

    // goob edit
    public bool StartAnnounced = false;

    /// <summary>
    /// Trauma - After this percentage of crew are zombies, a CBurn shuttle will be automatically sent.
    /// </summary>
    [DataField]
    public float ZombieCBurnCallPercentage = 0.6f;

    /// <summary>
    /// Trauma - The shuttle event used for the zombies CBurn autocall.
    /// </summary>
    [DataField]
    public EntProtoId ZombieCBurnEvent = "SpawnCBURNNoAnnounce";

    /// <summary>
    /// Trauma - Whether or not a CBurn shuttle for zombies has been sent.
    /// </summary>
    [DataField]
    public bool ZombieCBurnCalled;
}
