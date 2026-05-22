using Content.Shared.Whitelist;

namespace Content.Trauma.Shared.Heretic.Components.Side;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LionhunterRifleComponent : Component
{
    [DataField]
    public TimeSpan AimTimePerDistance = TimeSpan.FromMilliseconds(200);

    [DataField]
    public TimeSpan MaxAimTime = TimeSpan.FromSeconds(10);

    [DataField]
    public float MinDistance = 4f;

    [DataField]
    public float MaxDistance = 60f;

    [DataField]
    public EntProtoId AimMarkerProto = "LionhunterReticle";

    [DataField, AutoNetworkedField]
    public EntityUid? AimMarker;

    [DataField]
    public EntityWhitelist? AimWhitelist;
}
