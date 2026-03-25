// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Void;

[RegisterComponent, NetworkedComponent]
public sealed partial class VoidPrisonComponent : Component
{
    [DataField]
    public EntProtoId EndEffect = "EffectVoidPrisonEnd";
}
