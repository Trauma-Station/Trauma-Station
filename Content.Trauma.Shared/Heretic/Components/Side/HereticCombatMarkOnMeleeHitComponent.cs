// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Heretic.Components.Side;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HereticCombatMarkOnMeleeHitComponent : Component
{
    [DataField, AutoNetworkedField]
    public HereticPath NextPath = HereticPath.Ash;
}
