// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Xenomorph;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NeurotoxinGlandComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Active = false;

    /// <summary>
    /// What action.
    /// </summary>
    [DataField]
    public EntProtoId ActionId = "ActionAcidSpit";

    [DataField]
    public EntityUid? Action;
}
