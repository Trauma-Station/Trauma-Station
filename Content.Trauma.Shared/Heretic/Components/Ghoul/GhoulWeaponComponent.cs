// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Heretic.Components.Ghoul;

[RegisterComponent, NetworkedComponent]
public sealed partial class GhoulWeaponComponent : Component
{
    [DataField]
    public LocId ExamineMessage = "ghoul-weapon-comp-examine";
}
