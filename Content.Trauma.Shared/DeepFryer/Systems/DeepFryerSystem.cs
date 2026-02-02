using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Storage.Components;
using Content.Trauma.Shared.DeepFryer.Components;
using Robust.Shared.Containers;

namespace Content.Trauma.Shared.DeepFryer.Systems;

public abstract class DeepFryerSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeepFryerComponent, EntInsertedIntoContainerMessage>(OnInsert);
        SubscribeLocalEvent<DeepFryerComponent, StorageCloseAttemptEvent>(OnTryClose);
        SubscribeLocalEvent<DeepFryerComponent, StorageAfterCloseEvent>(OnClose);
        SubscribeLocalEvent<DeepFryerComponent, StorageAfterOpenEvent>(OnOpen);
    }

    private void OnOpen(Entity<DeepFryerComponent> ent, ref StorageAfterOpenEvent args)
    {
        _appearance.SetData(ent.Owner, DeepFryerVisuals.Open, true);
        _appearance.SetData(ent.Owner, DeepFryerVisuals.Frying, false);
        _appearance.SetData(ent.Owner, DeepFryerVisuals.BigFrying, false);
    }

    private void OnClose(Entity<DeepFryerComponent> ent, ref StorageAfterCloseEvent args)
    {
        if (!TryComp<EntityStorageComponent>(ent.Owner, out var entStorage))
            return;

        foreach (var entity in entStorage.Contents.ContainedEntities)
        {
            if (!TryComp<ItemComponent>(entity, out var item) || item.Size == "Ginormous")
            {
                _appearance.SetData(ent.Owner, DeepFryerVisuals.BigFrying, true); // If it doesn't have an item component or the item is big then it's big yeah
                return;
            }
        }

        _appearance.SetData(ent.Owner, DeepFryerVisuals.Frying, true);
    }

    private void OnTryClose(Entity<DeepFryerComponent> ent, ref StorageCloseAttemptEvent args)
    {
        if (!TryComp<SolutionContainerManagerComponent>(ent.Owner, out _)
            || !_solutionContainer.TryGetSolution(ent.Owner,
                ent.Comp.FryerSolution,
                out _,
                out var deepFryerSolution)
            || deepFryerSolution.Volume <= 0.75)
        {
            args.Cancelled = true;
            _popup.PopupEntity(Loc.GetString("deep-fryer-not-enough-liquid"), ent.Owner);
        }
    }

    private void OnInsert(Entity<DeepFryerComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        DoFryerLogic(args.Entity, ent);
    }

    private void DoFryerLogic(EntityUid item, Entity<DeepFryerComponent> ent)
    {
        DeepFryItem(item, ent);

        if (TryComp<InventoryComponent>(item, out var inventory))
        {
            foreach (var slot in inventory.Containers)
            {
                if (slot.ContainedEntity != null)
                    DeepFryItem(slot.ContainedEntity.Value, ent);
            }
        }

    }

    private void DeepFryItem(EntityUid item, Entity<DeepFryerComponent> ent)
    {
        EntityManager.AddComponents(item, ent.Comp.ComponentsToAdd, false);
        EntityManager.RemoveComponents(item, ent.Comp.ComponentsToRemove);
    }
}
