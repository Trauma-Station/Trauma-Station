// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Heretic.Components.PathSpecific.Blade;

[RegisterComponent, NetworkedComponent]
public sealed partial class SilverMaelstromComponent : Component
{
    public override bool SessionSpecific => true;

    [DataField]
    public float RespawnCooldown = 7.5f;

    [DataField]
    public float RespawnTimer = 0f;

    [DataField]
    public List<EntityUid> ActiveBlades = new();

    [DataField]
    public int MaxBlades = 5;
}
