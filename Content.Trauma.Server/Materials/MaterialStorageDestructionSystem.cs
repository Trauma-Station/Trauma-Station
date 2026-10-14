// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Destructible;
using Content.Shared.Materials;
using Content.Server.Materials;

namespace Content.Trauma.Server.Materials;

/// <summary>
/// Spills materials stored in a material storage if it gets destroyed.
/// </summary>
public sealed partial class MaterialStorageDestructionSystem : EntitySystem
{
    [Dependency] private MaterialStorageSystem _material = default!;

    [SubscribeLocalEvent]
    private void OnDestruction(Entity<MaterialStorageComponent> ent, ref DestructionEventArgs args)
    {
        if (!ent.Comp.DropOnDeconstruct) // only used by sheetifier lol
            return;

        var coords = Transform(ent).Coordinates;
        foreach (var (material, amount) in ent.Comp.Storage)
        {
            _material.SpawnMultipleFromMaterial(amount, material, coords);
        }
    }
}
