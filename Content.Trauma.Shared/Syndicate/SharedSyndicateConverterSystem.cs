using System.Diagnostics.CodeAnalysis;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Materials;
using Content.Shared.Popups;
using Content.Trauma.Shared.Syndicate.Components;
using Robust.Shared.Containers;

namespace Content.Trauma.Shared.Syndicate;

public abstract partial class SharedSyndicateConverterSystem : EntitySystem
{
    //[Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    //[Dependency] protected MachinePartSystem MachinePart = default!;
    [Dependency] protected SharedAppearanceSystem Appearance = default!;
    [Dependency] protected SharedMaterialStorageSystem MaterialStorage = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<SyndicateConverterComponent, ItemSlotInsertAttemptEvent>(OnInsertAttempt);
    }

    private void OnInsertAttempt(Entity<SyndicateConverterComponent> ent, ref ItemSlotInsertAttemptEvent args)
    {
        if (args.Slot.ID != ent.Comp.SlotId || args.Cancelled)
            return;

        args.Cancelled = true;
    }

    /// <summary>
    /// Returns the converted item from the input that the syndicate converter will create.
    /// </summary>
    public bool TryGetConvertedPrototype(Entity<SyndicateConvertibleComponent>? item, [NotNullWhen(true)] out EntProtoId? prototype)
    {
        prototype = null;
        if (item == null)
            return prototype is not null;
        prototype = ((Entity<SyndicateConvertibleComponent>) item).Comp.ConvertTo;
        return prototype is not null;
    }

    /// <summary>
    /// Tries to get the cost to convert an item, fails if unable to convert it.
    /// </summary>
    /// <param name="entity">The conversion machine</param>
    /// <param name="inputItem">The item to convert.</param>
    /// <param name="cost">Cost to convert</param>
    public bool TryGetConversionCost(Entity<SyndicateConverterComponent> entity, Entity<SyndicateConvertibleComponent>? item, out Dictionary<string, int> cost)
    {
        cost = new();
        if (item is null)
            return false;
        Dictionary<ProtoId<MaterialPrototype>, int> baseCost;
        baseCost = ((Entity<SyndicateConvertibleComponent>) item).Comp.MaterialCost;

        foreach (var (mat, amount) in baseCost)
        {
            cost.TryAdd(mat, 0);
            cost[mat] -= (int) System.Math.Round(amount * entity.Comp.MaterialCostScale);
        }

        return true;
    }
}
