// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.BloodCult.Constructs.SoulShard;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class SoulShardComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool IsBlessed;

    [DataField]
    public Color BlessedLightColor = Color.LightCyan;

    [DataField]
    public EntProtoId ShadeProto = "ShadeCult";

    [DataField]
    public EntProtoId PurifiedShadeProto = "ShadeHoly";

    [DataField, AutoNetworkedField]
    public EntityUid? ShadeUid;
}
