using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.Components;

public sealed partial class ZombieRuleComponent
{
    [DataField]
    public bool StartAnnounced;

    /// <summary>
    /// After this percentage of crew are zombies, a CBurn shuttle will be automatically sent.
    /// </summary>
    [DataField]
    public float ZombieCBurnCallPercentage = 0.6f;

    /// <summary>
    /// The shuttle event used for the zombies CBurn autocall.
    /// </summary>
    [DataField]
    public EntProtoId ZombieCBurnEvent = "SpawnCBURNNoAnnounce";

    /// <summary>
    /// Whether or not a CBurn shuttle for zombies has been sent.
    /// </summary>
    [DataField]
    public bool ZombieCBurnCalled;
}
