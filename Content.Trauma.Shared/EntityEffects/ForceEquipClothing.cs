using Content.Shared.EntityEffects;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Spawns and equips an item to the target's inventory slot, then makes it unremoveable.
/// </summary>
public sealed partial class ForceEquipClothing : EntityEffectBase<ForceEquipClothing>
{
    /// <summary>
    /// The clothing item to spawn.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Spawn;

    [DataField(required: true)]
    public string Slot = string.Empty;

    [DataField]
    public bool Silent = true;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var name = prototype.Index(Spawn).Name;
        return Loc.GetString("entity-effect-guidebook-force-equip-clothing", ("chance", Probability), ("slot", Slot), ("item", name));
    }
}

public sealed class ForceEquipClothingEffectSystem : EntityEffectSystem<InventoryComponent, ForceEquipClothing>
{
    [Dependency] private readonly InventorySystem _inventory = default!;

    protected override void Effect(Entity<InventoryComponent> ent, ref EntityEffectEvent<ForceEquipClothing> args)
    {
        var item = PredictedSpawnAtPosition(args.Effect.Spawn, Transform(ent).Coordinates);
        var slot = args.Effect.Slot;
        var silent = args.Effect.Silent;
        if (!_inventory.TryEquip(ent, item, slot, silent, force: true, predicted: true, ent.Comp))
        {
            PredictedDel(item);
            return;
        }

        EnsureComp<UnremoveableComponent>(item);
    }
}
