// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Goobstation.Shared.Clothing.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class CoatedClothingComponent : Component
{
    public List<string> CoatingNames = new List<string>();
}
