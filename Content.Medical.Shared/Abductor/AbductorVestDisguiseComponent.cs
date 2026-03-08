// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Medical.Shared.Abductor;

[RegisterComponent]
public sealed partial class AbductorVestDisguiseComponent : Component
{
}

[RegisterComponent]
public sealed partial class AbductorDisguiseStateComponent : Component
{
    [DataField]
    public string? OriginalName;

    [DataField]
    public Dictionary<EntityUid, PrototypeLayerData>? OriginalOrganData;
}
