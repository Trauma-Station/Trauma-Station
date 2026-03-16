using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Shitcode.Heretic.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class CosmosPassiveComponent : Component
{
    public override bool SessionSpecific => true;

    [DataField]
    public float StaminaHeal = -15f;

    // Storing ref to star gazer here cause why not
    [DataField, AutoNetworkedField]
    public EntityUid? StarGazer;

    [DataField]
    public EntProtoId StarGazerId = "MobStarGazer";

    [DataField]
    public TimeSpan UpdateDelay = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer:typeof(TimeOffsetSerializer)), AutoPausedField, AutoNetworkedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;
}
