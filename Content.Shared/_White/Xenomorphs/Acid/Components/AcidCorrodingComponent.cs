using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._White.Xenomorphs.Acid.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentPause]
public sealed partial class AcidCorrodingComponent : Component
{
    [DataField]
    public DamageSpecifier DamagePerSecond;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan AcidExpiresAt;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextDamageAt;

    [DataField]
    public EntityUid Acid;
}
