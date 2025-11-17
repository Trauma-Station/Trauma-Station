using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Shadowling.Components.Abilities.PreAscension;

/// <summary>
/// This is used for the Glare Ability
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ShadowlingGlareComponent : Component
{
    [DataField]
    public EntProtoId ActionId = "ActionGlare";

    [DataField]
    public EntityUid? ActionEnt;

    // <summary>
    // Variable stun time. On distance 1 or lower, it is maximized to 4 seconds of stun (enough to Enthrall),
    // otherwise it gets reduced based on distance.
    // </summary>
    [DataField]
    public TimeSpan GlareStunTime;

    // <summary>
    // Variable activation time. On distance 1 or lower, it is immediate,
    // otherwise it gets increased based on distance.
    // Max time before stun is 2 seconds
    // Note: needs to be rewritten to not use frametime
    // </summary>
    [DataField]
    public TimeSpan GlareTimeBeforeEffect;

    [DataField]
    public float MaxGlareDistance = 10f;

    [DataField]
    public float MinGlareDistance = 1f;

    [DataField]
    public TimeSpan MaxGlareStunTime = TimeSpan.FromSeconds(6);

    [DataField]
    public EntProtoId SlowdownStatusEffect = "ShadowlingGlareStatusEffect";

    [DataField]
    public TimeSpan SlowTime = TimeSpan.FromSeconds(7);

    [DataField]
    public TimeSpan MuteTime = TimeSpan.FromSeconds(6);

    // <summary>
    // Regarding time delay before activation
    // </summary>
    [DataField]
    public TimeSpan MaxGlareDelay = TimeSpan.FromSeconds(2);

    // <summary>
    // Regarding time delay before activation
    // </summary>
    [DataField]
    public TimeSpan MinGlareDelay = TimeSpan.FromSeconds(0.1f);

    [DataField]
    public EntityUid GlareTarget;

    [DataField]
    public bool ActivateGlareTimer;

    [DataField]
    public EntProtoId EffectGlare = "ShadowlingGlareEffect";
}
