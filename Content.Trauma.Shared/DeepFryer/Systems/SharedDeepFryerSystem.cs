// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Audio;
using Content.Shared.Body;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Coordinates;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Mind.Components;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Storage.Components;
using Content.Trauma.Shared.DeepFryer.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.DeepFryer.Systems;

public abstract class SharedDeepFryerSystem : EntitySystem
{
    [Dependency] protected readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] protected readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly NameModifierSystem _nameModifier = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _power = default!;

    public static readonly ProtoId<ItemSizePrototype> Ginormous = "Ginormous";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeepFryerComponent, StorageCloseAttemptEvent>(OnTryClose);
        SubscribeLocalEvent<DeepFryerComponent, StorageAfterCloseEvent>(OnClose);
        SubscribeLocalEvent<DeepFryerComponent, StorageAfterOpenEvent>(OnOpen);
        SubscribeLocalEvent<DeepFryerComponent, PowerChangedEvent>(OnPowerChanged);

        SubscribeLocalEvent<ActiveDeepFryerComponent, ComponentStartup>(OnActivated);
        SubscribeLocalEvent<ActiveDeepFryerComponent, ComponentShutdown>(OnDeactivated);
    }

    private void OnOpen(Entity<DeepFryerComponent> ent, ref StorageAfterOpenEvent args)
    {
        _appearance.SetData(ent.Owner, DeepFryerVisuals.Open, true);
        Deactivate(ent);
    }

    private void OnClose(Entity<DeepFryerComponent> ent, ref StorageAfterCloseEvent args)
    {
        _appearance.SetData(ent.Owner, DeepFryerVisuals.Open, false);
        TryActivate(ent);
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

        ent.Comp.LastUser = args.User;
    }

    private void OnPowerChanged(Entity<DeepFryerComponent> ent, ref PowerChangedEvent args)
    {
        if (!args.Powered)
            Deactivate(ent);
        // doesn't automatically turn back on when repowered
    }

    private void OnActivated(Entity<ActiveDeepFryerComponent> ent, ref ComponentStartup args)
    {
        _ambientSound.SetAmbience(ent.Owner, true);
        _appearance.SetData(ent.Owner, DeepFryerVisuals.Frying, true);
    }

    private void OnDeactivated(Entity<ActiveDeepFryerComponent> ent, ref ComponentShutdown args)
    {
        _ambientSound.SetAmbience(ent.Owner, false);
        _appearance.SetData(ent.Owner, DeepFryerVisuals.Frying, false);
        _appearance.SetData(ent.Owner, DeepFryerVisuals.BigFrying, false);
    }

    #region Helper Methods
    private void TryActivate(Entity<DeepFryerComponent> ent)
    {
        if (!_power.IsPowered(ent.Owner))
            return;

        EnsureComp<ActiveDeepFryerComponent>(ent);
        _audio.Stop(ent.Comp.Sound);
        ent.Comp.Sound = _audio.PlayPredicted(ent.Comp.StartSound, ent.Owner, ent.Owner)?.Entity;
        ent.Comp.FryFinishTime = _timing.CurTime + ent.Comp.TimeToDeepFry;

        if (!TryComp<EntityStorageComponent>(ent.Owner, out var entStorage))
            return;

        foreach (var entity in entStorage.Contents.ContainedEntities)
        {
            ent.Comp.StoredObjects.Add(entity);
            if (!TryComp<ItemComponent>(entity, out var item) || item.Size == Ginormous)
            {
                _appearance.SetData(ent.Owner, DeepFryerVisuals.BigFrying, true); // If it doesn't have an item component or the item is big then it's big yeah
                return;
            }
        }
    }

    private void Deactivate(Entity<DeepFryerComponent> ent)
    {
        ent.Comp.LastUser = null;

        if (!RemComp<ActiveDeepFryerComponent>(ent))
            return;

        _audio.Stop(ent.Comp.Sound);
        ent.Comp.Sound = _audio.PlayPredicted(ent.Comp.FinishSound, ent.Owner, ent.Owner)?.Entity;
        ent.Comp.StoredObjects.Clear();
        ent.Comp.FryFinishTime = TimeSpan.Zero;

        if (TryComp<SolutionContainerManagerComponent>(ent.Owner, out _)
            && _solution.TryGetSolution(ent.Owner,
                ent.Comp.FryerSolutionContainer,
                out var solution,
                out _))
            _solution.SetTemperature(solution.Value, 293.7f); // Reset the temp when its opened
    }

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
        if (HasComp<DeepFriedComponent>(item))
            return;

        EntityManager.AddComponents(item, ent.Comp.ComponentsToAdd, false);
        EntityManager.RemoveComponents(item, ent.Comp.ComponentsToRemove);
        if (!HasComp<BodyComponent>(item))
        {
            EntityManager.AddComponents(item, ent.Comp.ComponentsToAddObjects, false);
            EntityManager.RemoveComponents(item, ent.Comp.ComponentsToRemoveObjects);

            foreach (var container in ent.Comp.ContainersToRemove)
            {
                if (_container.TryGetContainer(item, container, out var containerId))
                {
                    _container.EmptyContainer(containerId);
                    _container.ShutdownContainer(containerId);
                }
            }

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
