// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Goobstation.Shared.Clothing.Components;

[RegisterComponent]
public sealed partial class ClothingGrantComponentComponent : Component
{
    [DataField("component", required: true)]
    [AlwaysPushInheritance]
    public ComponentRegistry Components = new();

    [DataField]
    public HashSet<string> Active = new();
}
