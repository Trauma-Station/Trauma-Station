using Content.Shared.Audio;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Storage.Components;
using Content.Trauma.Shared.DeepFryer.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.DeepFryer.Systems;

public abstract class DeepFryerSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private ProtoId<DamageTypePrototype> damageType = "Heat";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeepFryerComponent, StorageCloseAttemptEvent>(OnTryClose);
        SubscribeLocalEvent<DeepFryerComponent, StorageAfterCloseEvent>(OnClose);
        SubscribeLocalEvent<DeepFryerComponent, StorageAfterOpenEvent>(OnOpen);
    }

    private void OnOpen(Entity<DeepFryerComponent> ent, ref StorageAfterOpenEvent args)
    {

        _ambientSound.SetAmbience(ent.Owner, false);
        _audio.PlayPredicted(ent.Comp.FinishSound, ent.Owner, ent.Owner);
        ent.Comp.StoredObjects.Clear();
        ent.Comp.FryFinishTime = TimeSpan.Zero;
        _appearance.SetData(ent.Owner, DeepFryerVisuals.Open, true);
        _appearance.SetData(ent.Owner, DeepFryerVisuals.Frying, false);
        _appearance.SetData(ent.Owner, DeepFryerVisuals.BigFrying, false);
    }

    private void OnClose(Entity<DeepFryerComponent> ent, ref StorageAfterCloseEvent args)
    {
        if (!TryComp<EntityStorageComponent>(ent.Owner, out var entStorage))
            return;

        _ambientSound.SetAmbience(ent.Owner, true);
        _audio.PlayPredicted(ent.Comp.StartSound, ent.Owner, ent.Owner);
        ent.Comp.FryFinishTime = _gameTiming.CurTime + ent.Comp.TimeToDeepFry;
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

    protected void AddHeatDamage(DeepFryerComponent comp, float frameTime)
    {
        var heatProto = _prototypeManager.Index(damageType);

        foreach (var entity in comp.StoredObjects)
        {
            if (!TryComp<DamageableComponent>(entity, out _))
                continue;

            _damageable.TryChangeDamage(entity, new DamageSpecifier(heatProto, comp.HeatDamage * frameTime));
        }
    }

    protected void DeepFryItems(Entity<DeepFryerComponent> ent)
    {
        foreach (var storedObject in ent.Comp.StoredObjects)
        {
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
    }
}
