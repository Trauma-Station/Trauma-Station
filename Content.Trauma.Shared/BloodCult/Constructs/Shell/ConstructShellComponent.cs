// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Containers.ItemSlots;
using Content.Trauma.Common.RadialSelector;

namespace Content.Trauma.Shared.BloodCult.Constructs.Shell;

[RegisterComponent, NetworkedComponent]
public sealed partial class ConstructShellComponent : Component
{
    [DataField(required: true)]
    public ItemSlot ShardSlot = default!;

    [DataField]
    public string ShardSlotId = "Shard";

    [DataField]
    public List<RadialSelectorEntry> Constructs = new()
    {
        new() { Prototype = "ConstructJuggernaut", },
        new() { Prototype = "ConstructArtificer", },
        new() { Prototype = "ConstructWraith", }
    };

    [DataField]
    public List<RadialSelectorEntry> PurifiedConstructs = new()
    {
        new() { Prototype = "ConstructJuggernautHoly", },
        new() { Prototype = "ConstructArtificerHoly", },
        new() { Prototype = "ConstructWraithHoly", }
    };
}
