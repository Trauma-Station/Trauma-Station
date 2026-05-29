// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Heretic.Systems.PathSpecific.Ash;

namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Ash;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CrackedLanternComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Summoned;

    [DataField]
    public EntProtoId<CrackedLanternSummonComponent> SummonProto = "LanternHereticSummon";
}
