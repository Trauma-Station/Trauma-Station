// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Atmos;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Trauma.Shared.Decapoids;

[RegisterComponent]
[AutoGenerateComponentPause]
public sealed partial class VaporizerComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string LiquidTank = "waterTank";

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<ReagentPrototype> ExpectedReagent = "Water";

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public Gas OutputGas = Gas.WaterVapor;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MaxPressure = Atmospherics.OneAtmosphere * 10;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ReagentToMoles = 0.07f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ReagentPerSecond = 0.09f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan ProcessDelay = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextProcess = new();
}
