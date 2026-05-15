// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Viewcone;

/// <summary>
/// Spawns a visual effect shown outside your vision cone when this entity does a melee attack or disarm.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ViewconeMeleeEffectComponent : Component
{
    [DataField]
    public EntProtoId Effect = "ViewconeEffectAttack";
}
