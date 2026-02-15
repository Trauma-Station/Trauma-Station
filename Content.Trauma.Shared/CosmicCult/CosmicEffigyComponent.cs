using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.CosmicCult;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentPause]
public sealed partial class CosmicEffigyComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan EffectTimer = default!;

    [DataField] public TimeSpan EffectTime = TimeSpan.FromSeconds(50);

    [DataField] public EntProtoId ActivationVfx = "CosmicGenericVFX";

    [DataField] public SoundSpecifier ActivationSfx = new SoundPathSpecifier("/Audio/_DV/CosmicCult/effigy_pulse.ogg");

    [DataField] public float LightShatterRange = 5f;

    [DataField] public float LightShatterRangeCap = 35f;

    [DataField] public EntProtoId CustodianProto = "MobCosmicCustodian";

    [DataField] public EntProtoId LodestarProto = "MobCosmicLodestar";

    [DataField] public int CustodianCap = 6;

    [DataField] public int LodestarCap = 3;

    [DataField] public HashSet<EntityUid> SummonedCustodians = [];

    [DataField] public HashSet<EntityUid> SummonedLodestars = [];
}
