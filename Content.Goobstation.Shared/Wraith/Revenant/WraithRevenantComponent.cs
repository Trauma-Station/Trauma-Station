// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;

namespace Content.Goobstation.Shared.Wraith.Revenant;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class WraithRevenantComponent : Component
{
    [ViewVariables]
    public EntProtoId RevenantAbilities = "RevenantAbilities";

    [ViewVariables, AutoNetworkedField]
    public DamageSpecifier? OldDamageSpecifier;

    [ViewVariables, AutoNetworkedField]
    public bool HadPassive;
}
