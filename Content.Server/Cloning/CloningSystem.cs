// <Trauma>
using Content.Goobstation.Common.Cloning;
using Content.Goobstation.Shared.CloneProjector.Clone;
using Content.Goobstation.Shared.Clothing.Components;
using Content.Goobstation.Shared.Clothing.Systems;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Interaction.Components;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Radio.Components;
using Content.Shared.Radio.EntitySystems;
using Robust.Shared.Utility;
// </Trauma>
using Content.Shared.Administration.Logs;
using Content.Shared.Body;
using Content.Shared.Cloning;
using Content.Shared.Cloning.Events;
using Content.Shared.Database;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.StatusEffect;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Server.Cloning;

/// <summary>
///     System responsible for making a copy of a humanoid's body.
///     For the cloning machines themselves look at CloningPodSystem, CloningConsoleSystem and MedicalScannerSystem instead.
/// </summary>
public sealed partial class CloningSystem : SharedCloningSystem
{
    // <Trauma>
    [Dependency] private ToggleableClothingSystem _toggleable = default!;
    // TODO: decouple this shitcode
    [Dependency] private SealableClothingSystem _sealable = default!;
    // </Trauma>
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedStorageSystem _storage = default!;
    [Dependency] private SharedSubdermalImplantSystem _subdermalImplant = default!;
    [Dependency] private SharedVisualBodySystem _visualBody = default!;
    [Dependency] private NameModifierSystem _nameMod = default!;
    [Dependency] private IdentitySystem _identity = default!;

    public override bool TryCloning(
        EntityUid original,
        MapCoordinates? coords,
        ProtoId<CloningSettingsPrototype> settingsId,
        [NotNullWhen(true)] out EntityUid? clone)
    {
        clone = null;
        if (!ProtoMan.Resolve(settingsId, out var settings))
            return false; // invalid settings

        // Goobstation start - non humanoid cloning
        if (!TryComp<HumanoidProfileComponent>(original, out var humanoid) && !settings.AllowNonHumanoid)
            return false; // whatever body was to be cloned, was not a humanoid

        SpeciesPrototype? speciesPrototype = null;
        if (humanoid != null && !ProtoMan.Resolve(humanoid.Species, out speciesPrototype))
            return false; // invalid species

        var proto = speciesPrototype?.Prototype.ToString() ?? Prototype(original)?.ID;
        if (proto == null)
            return false;
        // Goobstation end

        if (!settings.ForceCloning)
        {
            var attemptEv = new CloningAttemptEvent(settings);
            RaiseLocalEvent(original, ref attemptEv);
            if (attemptEv.Cancelled)
                return false; // cannot clone, for example due to the unrevivable trait
        }

        clone = coords == null ? Spawn(proto) : Spawn(proto, coords.Value); // Goob - use proto from above
        _visualBody.CopyAppearanceFrom(original, clone.Value);

        CloneComponents(original, clone.Value, settings);

        // Add equipment first so that SetEntityName also renames the ID card.
        if (settings.CopyEquipment != null)
            CopyEquipment(original, clone.Value, settings.CopyEquipment.Value, settings.EquipmentWhitelist, settings.EquipmentBlacklist,
                settings.MakeEquipmentUnremoveable, settings.CopyStorage, settings.InternalContentsUnremoveable); // Trauma

        // Copy storage on the mob itself as well.
        // This is needed for slime storage.
        if (settings.CopyInternalStorage)
            CopyStorage(original, clone.Value, settings.EquipmentWhitelist, settings.EquipmentBlacklist);

        // copy implants and their storage contents
        if (settings.CopyImplants)
            CopyImplants(original, clone.Value, settings.CopyInternalStorage, settings.EquipmentWhitelist, settings.EquipmentBlacklist);

        // Copy permanent status effects
        if (settings.CopyStatusEffects)
            CopyStatusEffects(original, clone.Value, settings.StatusEffectWhitelist, settings.StatusEffectBlacklist);

        var originalName = _nameMod.GetBaseName(original);

        // Set the clone's name. The raised events will also adjust their PDA and ID card names.
        _metaData.SetEntityName(clone.Value, originalName, raiseEvents: settings.RaiseEntityRenamedEvent);
        _identity.QueueIdentityUpdate(clone.Value); // We have to manually refresh the identity in case we did not raise events.

        _adminLogger.Add(LogType.Chat, LogImpact.Medium, $"The body of {original:player} was cloned as {clone.Value:player}");
        return true;
    }

    public override void CloneComponents(
        EntityUid original,
        EntityUid clone,
        ProtoId<CloningSettingsPrototype> settings)
    {
        if (!ProtoMan.Resolve(settings, out var proto))
            return;

        CloneComponents(original, clone, proto);
    }

    public override void CloneComponents(
        EntityUid original,
        EntityUid clone,
        CloningSettingsPrototype settings)
    {
        var componentsToCopy = settings.Components;
        var componentsToEvent = settings.EventComponents;

        // don't make status effects permanent
        if (TryComp<StatusEffectsComponent>(original, out var statusComp))
        {
            var statusComps = statusComp.ActiveEffects.Values.Select(s => s.RelevantComponent).Where(s => s != null).ToList();
            componentsToCopy.ExceptWith(statusComps!);
            componentsToEvent.ExceptWith(statusComps!);
        }

        // Goobstation Start
        // Ensure EncryptionKeyHolderComponent is in the components to copy if it exists on the original
        if (HasComp<EncryptionKeyHolderComponent>(original) &&
            TryComp<EncryptionKeyHolderComponent>(original, out var originalKeyHolder) &&
            TryComp<EncryptionKeyHolderComponent>(clone, out var cloneKeyHolder))
        {
            // The component is already copied by the cloning system, we just need to copy the keys
            var originalContainer = originalKeyHolder.KeyContainer;
            var cloneContainer = cloneKeyHolder.KeyContainer;

            // Clear any existing keys in the clone
            _container.CleanContainer(cloneContainer);

            // Copy each key from original to clone
            foreach (var key in originalContainer.ContainedEntities.ToList())
            {
                if (!Exists(key))
                    continue;

                // Create a new instance of the key
                if (MetaData(key).EntityPrototype is { } proto)
                {
                    var newKey = Spawn(proto.ID, Transform(clone).Coordinates);
                    if (!_container.Insert(newKey, cloneContainer))
                    {
                        Log.Warning($"Failed to insert key {ToPrettyString(newKey)} into clone's container");
                        Del(newKey);
                    }
                }
            }
            // Update the encryption channels on the clone
            var encSystem = EntityManager.System<EncryptionKeySystem>();
            encSystem.UpdateChannels(clone, cloneKeyHolder);
        }
        // Goobstation End

        foreach (var componentName in componentsToCopy)
        {
            if (!Factory.TryGetRegistration(componentName, out var componentRegistration))
            {
                Log.Error($"Tried to use invalid component registration for cloning: {componentName}");
                continue;
            }

            // If the original does not have the component, then the clone shouldn't have it either.
            RemComp(clone, componentRegistration.Type);
            if (TryComp(original, componentRegistration.Type, out var sourceComp)) // Does the original have this component?
            {
                CopyComp(original, clone, sourceComp);
            }
        }

        foreach (var componentName in componentsToEvent)
        {
            if (!Factory.TryGetRegistration(componentName, out var componentRegistration))
            {
                Log.Error($"Tried to use invalid component registration for cloning: {componentName}");
                continue;
            }

            // If the original does not have the component, then the clone shouldn't have it either.
            if (!HasComp(original, componentRegistration.Type))
                RemComp(clone, componentRegistration.Type);
        }

        var cloningEv = new CloningEvent(settings, clone);
        RaiseLocalEvent(original, ref cloningEv); // used for datafields that cannot be directly copied using CopyComp
    }

    public override void CopyEquipment(
        Entity<InventoryComponent?> original,
        Entity<InventoryComponent?> clone,
        SlotFlags slotFlags,
        EntityWhitelist? whitelist = null,
        EntityWhitelist? blacklist = null,
        bool makeUnremoveable = false, bool copyStorage = true, bool internalContentsUnremoveable = false) // Trauma
    {
        if (!Resolve(original, ref original.Comp) || !Resolve(clone, ref clone.Comp))
            return;

        var coords = Transform(clone).Coordinates;

        // Iterate over all inventory slots
        var slotEnumerator = _inventory.GetSlotEnumerator(original, slotFlags);
        while (slotEnumerator.NextItem(out var item, out var slot))
        {
            var cloneItem = CopyItem(item, coords, whitelist, blacklist, copyStorage); // Trauma - pass copyStorage

            // Goob edit start
            if (cloneItem == null)
                continue;

            if (!_inventory.TryEquip(clone, cloneItem.Value, slot.Name, silent: true, inventory: clone.Comp))
            {
                Del(cloneItem); // delete it again if the clone cannot equip it
                continue;
            }

            if (makeUnremoveable)
                EnsureComp<UnremoveableComponent>(cloneItem.Value);

            if (internalContentsUnremoveable && TryComp(cloneItem.Value, out ContainerManagerComponent? manager))
            {
                foreach (var container in manager.Containers.Values)
                {
                    foreach (var contained in container.ContainedEntities)
                    {
                        if (!HasComp<AttachedClothingComponent>(contained))
                            EnsureComp<UnremoveableComponent>(contained);
                    }
                }
            }

            if (!TryComp(item, out ToggleableClothingComponent? toggleable) || toggleable.ClothingUids.Count == 0 ||
                !TryComp(cloneItem.Value, out ToggleableClothingComponent? clonedToggleable))
                continue;

            var allEquipped = true;
            List<EntityUid> equipped = new();
            foreach (var (clothing, toggleSlot) in toggleable.ClothingUids)
            {
                if (!_toggleable.IsToggled((item, toggleable), clothing))
                {
                    allEquipped = false;
                    continue;
                }

                if (clonedToggleable.ClothingUids.FirstOrNull(kvp => kvp.Value == toggleSlot) is not
                    { } newClothing)
                {
                    allEquipped = false;
                    continue;
                }

                if (_toggleable.EquipClothing(clone.Owner,
                        (cloneItem.Value, clonedToggleable),
                        newClothing.Key,
                        newClothing.Value,
                        true))
                    equipped.Add(newClothing.Key);
            }

            if (!allEquipped || !TryComp(item, out SealableClothingControlComponent? sealable) ||
                !sealable.IsCurrentlySealed ||
                !TryComp(cloneItem.Value, out SealableClothingControlComponent? clonedSealable))
                continue;

            var success = true;
            foreach (var toSeal in equipped)
            {
                if (!_sealable.SealPart(toSeal, (cloneItem.Value, clonedSealable), true))
                {
                    success = false;
                    break;
                }
            }

            if (success)
                _sealable.EndSealProcess((cloneItem.Value, clonedSealable), true);
            // Goob edit end
        }
    }

    public override EntityUid? CopyItem(
        EntityUid original,
        EntityCoordinates coords,
        EntityWhitelist? whitelist = null,
        EntityWhitelist? blacklist = null,
        bool copyStorage = true) // Trauma
    {
        // we use a whitelist and blacklist to be sure to exclude any problematic entities
        if (!_whitelist.CheckBoth(original, blacklist, whitelist))
            return null;

        var prototype = MetaData(original).EntityPrototype?.ID;
        if (prototype == null)
            return null;

        var spawned = SpawnAtPosition(prototype, coords);

        // copy over important component data
        var ev = new CloningItemEvent(spawned);
        RaiseLocalEvent(original, ref ev);

        // if the original has items inside its storage, copy those as well
        if (TryComp<StorageComponent>(original, out var originalStorage) && TryComp<StorageComponent>(spawned, out var spawnedStorage)) // Goob edit
        {
            // remove all items that spawned with the entity inside its storage
            // this ignores other containers, but this should be good enough for our purposes
            _container.CleanContainer(spawnedStorage.Container);

            if (!copyStorage) // Goobstation
                return spawned;

            // recursively replace them
            // surely no one will ever create two items that contain each other causing an infinite loop, right?
            foreach ((var itemUid, var itemLocation) in originalStorage.StoredItems)
            {
                var copy = CopyItem(itemUid, coords, whitelist, blacklist);
                if (copy != null)
                    _storage.InsertAt((spawned, spawnedStorage), copy.Value, itemLocation, out _, playSound: false);
            }
        }

        return spawned;
    }

    public override void CopyStorage(
        Entity<StorageComponent?> original,
        Entity<StorageComponent?> target,
        EntityWhitelist? whitelist = null,
        EntityWhitelist? blacklist = null)
    {
        if (!Resolve(original, ref original.Comp, false) || !Resolve(target, ref target.Comp, false))
            return;

        var coords = Transform(target).Coordinates;

        // delete all items in the target storage
        _container.CleanContainer(target.Comp.Container);

        // recursively replace them
        foreach ((var itemUid, var itemLocation) in original.Comp.StoredItems)
        {
            var copy = CopyItem(itemUid, coords, whitelist, blacklist);
            if (copy != null)
                _storage.InsertAt(target, copy.Value, itemLocation, out _, playSound: false);
        }
    }

    public override void CopyImplants(
        Entity<ImplantedComponent?> original,
        EntityUid target,
        bool copyStorage = false,
        EntityWhitelist? whitelist = null,
        EntityWhitelist? blacklist = null)
    {
        if (!Resolve(original, ref original.Comp, false))
            return; // they don't have any implants to copy!

        foreach (var originalImplant in original.Comp.ImplantContainer.ContainedEntities)
        {
            if (!HasComp<SubdermalImplantComponent>(originalImplant))
                continue; // not an implant (should only happen with admin shenanigans)

            var implantId = MetaData(originalImplant).EntityPrototype?.ID;

            if (implantId == null)
                continue;

            var targetImplant = _subdermalImplant.AddImplant(target, implantId);

            if (targetImplant == null)
                continue;

            // copy over important component data
            var ev = new CloningItemEvent(targetImplant.Value);
            RaiseLocalEvent(originalImplant, ref ev);

            if (copyStorage)
                CopyStorage(originalImplant, targetImplant.Value, whitelist, blacklist); // only needed for storage implants
        }
    }
}
