// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Server.BloodCult.Objectives;

[RegisterComponent, Access(typeof(KillTargetCultSystem))]
public sealed partial class KillTargetCultComponent : Component
{
    [DataField(required: true)]
    public LocId Title;

    [DataField]
    public EntityUid? Target;
}
