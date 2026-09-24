// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.BloodCult.Items;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ShuttleCurseProviderComponent : Component
{
    [DataField]
    public int MaxUses = 3;

    [DataField, AutoNetworkedField]
    public int CurrentUses;
}
