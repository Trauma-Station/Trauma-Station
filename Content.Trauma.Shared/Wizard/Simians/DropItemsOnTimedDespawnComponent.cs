// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.Wizard.Simians;

[RegisterComponent, NetworkedComponent]
public sealed partial class DropItemsOnTimedDespawnComponent : Component
{
    [DataField]
    public bool DropDespawningItems;
}
