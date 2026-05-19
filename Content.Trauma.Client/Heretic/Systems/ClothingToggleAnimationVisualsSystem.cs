// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Clothing;
using Content.Client.Items.Systems;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Trauma.Shared.Heretic.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Trauma.Client.Heretic.Systems;

public sealed partial class ClothingToggleAnimationVisualsSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private ItemSystem _item = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClothingToggleAnimationVisualsComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<ClothingToggleAnimationVisualsComponent, GetEquipmentVisualsEvent>(OnGetEquipmentVisuals,
            after: new[] { typeof(ClientClothingSystem) });
    }

    private void OnGetEquipmentVisuals(Entity<ClothingToggleAnimationVisualsComponent> ent,
        ref GetEquipmentVisualsEvent args)
    {
        if (!TryComp(ent, out AppearanceComponent? appearance) || !_appearance.TryGetData(ent,
                ToggleAnimationVisuals.ToggleState,
                out ToggleAnimationState state,
                appearance) || (ent.Comp.State & state) == 0x0)
            return;

        if (!TryComp(ent, out ClothingComponent? clothing) || !TryComp(ent, out ToggleAnimationComponent? animation))
            return;

        var key = $"{state.ToString()}-{ClientClothingSystem.TemporarySlotMap[args.Slot]}";
        args.Layers.Add((key, new PrototypeLayerData { Loop = false, RsiPath = clothing.RsiPath, State = key }));
        args.LayersAnimationTime[key] =
            MathF.Max(0f, (float) (_timing.CurTime - animation.ToggleStartTime).TotalSeconds);
    }

    private void OnAppearanceChange(Entity<ClothingToggleAnimationVisualsComponent> ent, ref AppearanceChangeEvent args)
    {
        _item.VisualsChanged(ent);
    }
}
