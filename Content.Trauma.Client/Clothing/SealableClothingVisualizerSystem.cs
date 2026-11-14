// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Client.Clothing;
using Content.Shared.Clothing;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Trauma.Shared.Clothing.Sealable;

namespace Content.Trauma.Client.Clothing.Systems;

public sealed partial class SealableClothingVisualizerSystem : VisualizerSystem<SealableClothingVisualsComponent>
{
    [Dependency] private SharedItemSystem _item = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private EntityQuery<InventoryComponent> _inventoryQuery = default!;

    protected override void OnAppearanceChange(EntityUid uid, SealableClothingVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (!AppearanceSystem.TryGetData<bool>(uid, SealableClothingVisuals.Sealed, out var isSealed, args.Component))
            return;

        if (args.Sprite is { } sprite && component.SpriteLayer is { } layerKey &&
            _sprite.LayerMapTryGet((uid, sprite), layerKey, out var layer, false))
            _sprite.LayerSetVisible((uid, sprite), layer, isSealed);

        _item.VisualsChanged(uid);
    }

    [SubscribeLocalEvent(after: new[] { typeof(ClientClothingSystem) })]
    private void OnGetEquipmentVisuals(Entity<SealableClothingVisualsComponent> ent, ref GetEquipmentVisualsEvent args)
    {
        if (!AppearanceSystem.TryGetData<bool>(ent.Owner, SealableClothingVisuals.Sealed, out var isSealed) || !isSealed)
            return;

        if (!ent.Comp.ClothingVisuals.TryGetValue(args.Slot, out var layers))
            return;

        // attempt to get species specific data || if none found will use generic data instead
        if (_inventoryQuery.TryComp(args.Equipee, out var inventory) &&
            inventory.SpeciesId is { } species &&
            ent.Comp.ClothingVisuals.TryGetValue($"{args.Slot}-{species}", out var speciesLayers))
            layers = speciesLayers;

        var i = 0;
        foreach (var layer in layers)
        {
            if (layer.MapKeys?.FirstOrDefault() is not { } key)
            {
                key = i == 0 ? $"{args.Slot}-sealed" : $"{args.Slot}-sealed-{i}";
                i++;
            }

            args.Layers.Add((key, layer));
        }
    }
}
