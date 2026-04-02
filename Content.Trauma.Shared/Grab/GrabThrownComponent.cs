// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Content.Shared.Damage;

namespace Content.Trauma.Shared.Grab;

[RegisterComponent, NetworkedComponent]
public sealed partial class GrabThrownComponent : Component
{
    [DataField]
    public DamageSpecifier? DamageOnCollide;

    [DataField]
    public List<EntityUid> IgnoreEntity = new();
}
