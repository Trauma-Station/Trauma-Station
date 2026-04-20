// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;

namespace Content.Trauma.Shared.Wizard.LesserSummonGuns;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EnchantedBoltActionRifleComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public EntityUid? Caster;

    [DataField, AutoNetworkedField]
    public int Shots = 30;

    [DataField]
    public EntProtoId Proto = "WeaponBoltActionEnchanted";

    [DataField]
    public Vector2 ThrowingSpeed = new(2f, 4f);
}
