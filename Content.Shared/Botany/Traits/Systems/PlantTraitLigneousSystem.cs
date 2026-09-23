using Content.Shared.Botany.Components;
using Content.Shared.Botany.Events;
using Content.Shared.Botany.Systems;
using Content.Shared.Botany.Traits.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tools.Systems;

namespace Content.Shared.Botany.Traits.Systems;

/// <inheritdoc cref="PlantTraitLigneousComponent"/>
public sealed partial class PlantTraitLigneousSystem : EntitySystem
{
    [Dependency] private PlantHarvestSystem _plantHarvest = default!;
    [Dependency] private PlantHolderSystem _plantHolder = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedToolSystem _tool = default!;

    [Dependency] private EntityQuery<PlantHolderComponent> _holderQuery;

    [SubscribeLocalEvent]
    private void OnInteractUsing(Entity<PlantTraitLigneousComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!_holderQuery.TryComp(ent.Owner, out var holder))
            return;

        if (!holder.ReadyForHarvest)
            return;

        if (_plantHolder.IsDead(ent.Owner))
        {
            _popup.PopupCursor(Loc.GetString("plant-component-dead-plant-matter-message"), args.User); // Trauma - fix bad locid
            return;
        }

        // Ligneous requires sharp tool.
        var harvestToolQuality = ent.Comp.HarvestToolQuality;
        if (harvestToolQuality.HasValue && !_tool.HasQuality(args.Used, harvestToolQuality.Value))
            return;

        _plantHarvest.TryHandleHarvest(ent.Owner, args.User,
            tool: args.Used); // Trauma - pass the tool
        args.Handled = true;
    }

    [SubscribeLocalEvent(before: [typeof(PlantHarvestSystem)])]
    private void OnHarvestAttempt(Entity<PlantTraitLigneousComponent> ent, ref PlantHarvestAttemptEvent args)
    {
        // <Trauma>
        if (ent.Comp.HarvestToolQuality is not { } quality || args.Tool is { } tool && _tool.HasQuality(tool, quality))
            return;
        // </Trauma>
        _popup.PopupCursor(Loc.GetString("plant-component-ligneous-cant-harvest-message"), args.User);
        args.Cancelled = true;
    }
}
