// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.FixedPoint;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Trauma.Shared.Weapons.Ranged.Components;

[RegisterComponent]
public sealed partial class PlasmaAmmoProviderComponent : AmmoProviderComponent
{
    [DataField(required: true)]
    public EntProtoId Proto;

    [DataField]
    public FixedPoint2 FireCost = 55f;
}
