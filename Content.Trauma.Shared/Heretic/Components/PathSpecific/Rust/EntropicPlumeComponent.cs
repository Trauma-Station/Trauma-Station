// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;

namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Rust;

[RegisterComponent, NetworkedComponent]
public sealed partial class EntropicPlumeComponent : Component
{
    [DataField]
    public float Duration = 10f;

    [DataField]
    public Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> Reagents = new()
    {
        { "Mold", 5f },
    };

    [DataField]
    public List<EntityUid> AffectedEntities = new();
}
