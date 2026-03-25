// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Heretic.Components.Side;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class ForestAdmonitionsComponent : Component
{
    public override bool SessionSpecific => true;

    [DataField]
    public EntProtoId CloakEntity = "ShadowCloakEntityPale";

    [DataField]
    public EntProtoId FogProto = "HereticPaleFog";

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    [DataField]
    public TimeSpan UpdateDelay = TimeSpan.FromMilliseconds(250);

    [DataField]
    public int Range = 5;

    [DataField]
    public float FogSlope = 4f;
}
