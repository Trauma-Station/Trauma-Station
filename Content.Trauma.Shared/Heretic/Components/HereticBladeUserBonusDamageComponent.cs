// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Heretic.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class HereticBladeUserBonusDamageComponent : Component
{
    [DataField]
    public float BonusMultiplier = 0.5f;

    [DataField]
    public bool ApplyBladeEffects = true;

    [DataField]
    public HereticPath? Path = HereticPath.Flesh;
}
