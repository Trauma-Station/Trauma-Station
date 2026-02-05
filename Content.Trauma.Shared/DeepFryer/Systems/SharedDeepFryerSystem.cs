using Content.Shared.Audio;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Coordinates;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Mind.Components;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Storage.Components;
using Content.Trauma.Shared.DeepFryer.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.DeepFryer.Systems;

public abstract class SharedDeepFryerSystem : EntitySystem
{
    [Dependency] protected readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] protected readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly NameModifierSystem _nameModifier = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _power = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeepFryerComponent, StorageCloseAttemptEvent>(OnTryClose);
        SubscribeLocalEvent<DeepFryerComponent, StorageAfterCloseEvent>(OnClose);
        SubscribeLocalEvent<DeepFryerComponent, StorageAfterOpenEvent>(OnOpen);
    }

    private void OnOpen(Entity<DeepFryerComponent> ent, ref StorageAfterOpenEvent args)
    {
        ent.Comp.Closed = false;

        _ambientSound.SetAmbience(ent.Owner, false);
        _audio.PlayPredicted(ent.Comp.FinishSound, ent.Owner, ent.Owner);
        ent.Comp.StoredObjects.Clear();
        ent.Comp.FryFinishTime = TimeSpan.Zero;
        _appearance.SetData(ent.Owner, DeepFryerVisuals.Open, true);
        _appearance.SetData(ent.Owner, DeepFryerVisuals.Frying, false);
        _appearance.SetData(ent.Owner, DeepFryerVisuals.BigFrying, false);

        if (TryComp<SolutionContainerManagerComponent>(ent.Owner, out _)
            && _solution.TryGetSolution(ent.Owner,
                ent.Comp.FryerSolutionContainer,
                out var solution,
                out _))
            _solution.SetTemperature(solution.Value, 293.7f); // Reset the temp when its opened
    }

    private void OnClose(Entity<DeepFryerComponent> ent, ref StorageAfterCloseEvent args)
    {
        ent.Comp.Closed = true;

        if (!TryComp<EntityStorageComponent>(ent.Owner, out var entStorage))
            return;

        _ambientSound.SetAmbience(ent.Owner, true);
        _audio.PlayPredicted(ent.Comp.StartSound, ent.Owner, ent.Owner);
        ent.Comp.FryFinishTime = _timing.CurTime + ent.Comp.TimeToDeepFry;
        foreach (var entity in entStorage.Contents.ContainedEntities)
        {
            ent.Comp.StoredObjects.Add(entity);
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
            || !_solution.TryGetSolution(ent.Owner,
                ent.Comp.FryerSolutionContainer,
                out _,
                out var deepFryerSolution)
            || deepFryerSolution.Volume <= 100f)
        {
            args.Cancelled = true;
            _popup.PopupEntity(Loc.GetString("deep-fryer-not-enough-liquid"), ent.Owner);
            return;
        }

        if (!_power.IsPowered(ent.Owner))
        {
            args.Cancelled = true;
            _popup.PopupEntity(Loc.GetString("deep-fryer-no-power"), ent.Owner);
        }

    }

    #region Helper Methods
    protected void DeepFryItems(Entity<DeepFryerComponent> ent)
    {
        ent.Comp.FryFinishTime = _timing.CurTime + ent.Comp.TimeToDeepFry;

        _popup.PopupPredicted(Loc.GetString("deep-fryer-item-cooked"), ent.Owner, ent.Owner);

        foreach (var storedObject in ent.Comp.StoredObjects)
        {
            if (!Exists(storedObject) || HasComp<DeepFryerImmuneComponent>(storedObject))
                continue;

            if (HasComp<DeepFriedComponent>(storedObject) && !HasComp<MindContainerComponent>(storedObject)) // any twice deep-fried items get... OverCooked..? say that again
            {
                Spawn(ent.Comp.AshedItemToSpawn, ent.Owner.ToCoordinates());
                PredictedDel(storedObject);
                continue;
            }

            DeepFryItem(storedObject, ent);

            if (TryComp<InventoryComponent>(storedObject, out var inventory))
            {
                foreach (var slot in inventory.Containers)
                {
                    if (slot.ContainedEntity != null)
                        DeepFryItem(slot.ContainedEntity.Value, ent);
                }
            }
        }
    }

    private void DeepFryItem(EntityUid item, Entity<DeepFryerComponent> ent)
    {
        EntityManager.AddComponents(item, ent.Comp.ComponentsToAdd, false);
        EntityManager.RemoveComponents(item, ent.Comp.ComponentsToRemove);
        if (!HasComp<BodyComponent>(item))
        {
            EntityManager.AddComponents(item, ent.Comp.ComponentsToAddObjects, false);
        }

        EnsureComp<MetaDataComponent>(item, out var meta);

        var ev = new EntityRenamedEvent(item, meta.EntityName, Loc.GetString("deep-fried-item", ("name", meta.EntityName)));
        RaiseLocalEvent(item, ref ev, true);
        _nameModifier.RefreshNameModifiers(item);

        if (!_solution.TryGetSolution(item, ent.Comp.SolutionContainer, out var solutionRef, out var solution)
            || !_solution.TryGetSolution(ent.Owner, ent.Comp.FryerSolutionContainer, out var fryerSolution))
            return;

        var usedSolution = _solution.SplitSolution(fryerSolution.Value, ent.Comp.SolutionSpentPerFry); // spend a little solution to deep-fry

        _solution.SetCapacity(solutionRef.Value, solution.MaxVolume + ent.Comp.SolutionSpentPerFry);
        _solution.AddSolution(solutionRef.Value, usedSolution);
    }

    #endregion
}
