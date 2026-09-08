// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Atmos;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Items.Components;
using Content.Shared.Botany.Systems;
using Content.Shared.Botany.Traits.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Random;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Trauma.Shared.Botany.PlantAnalyzer;

public sealed partial class PlantAnalyzerSystem : EntitySystem
{
    [Dependency] private BotanySystem _botany = default!;
    [Dependency] private SharedAtmosphereSystem _atmos = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    [SubscribeLocalEvent]
    private void OnAfterInteract(Entity<PlantAnalyzerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target is not { } target || !args.CanReach || ent.Comp.Busy || !IsValidTarget(target))
            return;

        var delay = ent.Comp.Mode == PlantAnalyzerModes.Scan
            ? ent.Comp.ScanDelay
            : ent.Comp.ModeDelay;
        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, delay, new PlantAnalyzerDoAfterEvent(), ent, target: target, used: ent)
        {
            NeedHand = true,
            BreakOnDamage = true,
            BreakOnMove = true,
            MovementThreshold = 0.01f
        };
        ent.Comp.Busy = _doAfter.TryStartDoAfter(doAfterArgs);
    }

    [SubscribeLocalEvent]
    private void OnDoAfter(Entity<PlantAnalyzerComponent> ent, ref PlantAnalyzerDoAfterEvent args)
    {
        ent.Comp.Busy = false;
        if (args.Handled || args.Cancelled || args.Target is not { } target)
            return;

        var user = args.User;
        var mode = ent.Comp.Mode;
        if (mode == PlantAnalyzerModes.Scan)
            ScanPlant(ent, target, user);
        else if (mode == PlantAnalyzerModes.DeleteMutations)
            DeleteMutations(ent, target, user);
        else if (mode == PlantAnalyzerModes.Extract)
            ExtractGene(ent, target, user);
        else if (mode == PlantAnalyzerModes.Implant)
            InjectGene(ent, target, user);

        _ui.TryOpenUi(ent.Owner, PlantAnalyzerUiKey.Key, user);
        args.Handled = true;
    }

    private bool IsValidTarget(EntityUid target)
        => HasComp<SeedComponent>(target) ||
            HasComp<PlantDataComponent>(target) ||
            CompOrNull<PlantTrayComponent>(target)?.PlantEntity != null;

    private (EntityUid?, EntProtoId?) GetPlantData(EntityUid target)
    {
        if (TryComp<SeedComponent>(target, out var seed))
            return (seed.PlantData, seed.PlantProtoId);

        if (TryComp<PlantTrayComponent>(target, out var holder))
            return (holder.PlantEntity, null);

        if (HasComp<PlantDataComponent>(target))
            return (target, null);

        return (null, null);
    }

    private EntityUid EnsurePlantData(Entity<PlantAnalyzerComponent> ent, Entity<SeedComponent> seed)
    {
        if (seed.Comp.PlantData is { } uid)
            return uid;

        seed.Comp.PlantData = uid = EntityManager.PredictedSpawn(seed.Comp.PlantProtoId);
        Dirty(seed);

        // make sure the dummy plant entity is in pvs so clients can predict it
        var slot = _container.EnsureContainer<ContainerSlot>(seed.Owner, "seed_plant_data");
        _container.Insert(uid, slot);

        // let the UI update the seed's data without rescanning, if it was scanned already
        if (ent.Comp.Scanned == seed.Owner)
        {
            ent.Comp.Plant = uid;
            DirtyField(ent, ent.Comp, nameof(PlantAnalyzerComponent.Plant));
        }
        return uid;
    }

    public void ExtractGene(Entity<PlantAnalyzerComponent> ent, EntityUid target, EntityUid user)
    {
        // only requires a seed for balance chudding, deleting existing plants would be bad
        if (!HasComp<SeedComponent>(target) ||
            ent.Comp.GeneIndex < 0)
        {
            _popup.PopupEntity($"Collect seeds from it first!", ent, user);
            return;
        }

        GetGeneFromInteger(ent, target);
        PredictedQueueDel(target);

        _popup.PopupEntity($"Extracted and isolated gene from {Name(target)}", ent, user);
        _audio.PlayPredicted(ent.Comp.ExtractEndSound, ent, user);
    }

    public void InjectGene(Entity<PlantAnalyzerComponent> ent, EntityUid target, EntityUid user)
    {
        if (!TryComp<SeedComponent>(target, out var seed) ||
            ent.Comp.DatabankIndex < 0 ||
            // jesus christ
            ent.Comp.DatabankIndex >= ent.Comp.GeneBank.Count + ent.Comp.ConsumeGasesBank.Count + ent.Comp.ExudeGasesBank.Count + ent.Comp.ChemicalBank.Count)
        {
            _popup.PopupEntity($"Can't inject genes into germinated plants!", ent, user);
            return;
        }

        SetGeneFromInteger(ent, (target, seed));

        _popup.PopupEntity($"Injected gene into {Name(target)}", ent, user);
        _audio.PlayPredicted(ent.Comp.InjectEndSound, ent, user);
    }

    public void DeleteMutations(Entity<PlantAnalyzerComponent> ent, EntityUid target, EntityUid user)
    {
        if (!TryComp<SeedComponent>(target, out var seed))
        {
            _popup.PopupEntity($"Can't clear mutations from germinated plants!", ent, user);
            return;
        }

        var uid = EnsurePlantData(ent, (target, seed));
        var comp = Comp<PlantComponent>(uid);
        var name = Name(target);
        if (comp.Mutations.Count == 0)
        {
            _popup.PopupEntity($"There are no mutations to clear from {name}.", ent, user);
            return;
        }

        comp.Mutations.Clear();
        // it's not networked on the plant

        _popup.PopupEntity($"Cleared mutations of {name}!", ent, user);
        _audio.PlayPredicted(ent.Comp.DeleteMutationEndSound, ent, user);
    }

    public void ScanPlant(Entity<PlantAnalyzerComponent> ent, EntityUid target, EntityUid user)
    {
        ent.Comp.Scanned = target;
        (ent.Comp.Plant, ent.Comp.Seed) = GetPlantData(target);

        // mutations list isnt networked, have to do it ourselves
        ent.Comp.ScannedMutations.Clear();
        if (_botany.TryGetPlantComponent<PlantComponent>(ent.Comp.Plant, ent.Comp.Seed, out var plant))
        {
            foreach (var mutation in plant.Mutations)
            {
                ent.Comp.ScannedMutations.Add(mutation.Name);
            }
        }

        DirtyFields(ent, ent.Comp, null,
            nameof(PlantAnalyzerComponent.Scanned),
            nameof(PlantAnalyzerComponent.Plant),
            nameof(PlantAnalyzerComponent.Seed),
            nameof(PlantAnalyzerComponent.ScannedMutations));

        _popup.PopupEntity($"Scanned data of {Name(target)}.", ent, user);
        _audio.PlayPredicted(ent.Comp.ScanningEndSound, ent, user);
    }

    [SubscribeLocalEvent]
    private void OnModeSelected(Entity<PlantAnalyzerComponent> ent, ref PlantAnalyzerSetMode args)
    {
        if (ent.Comp.Busy || ent.Comp.Mode == args.Mode)
            return;

        ent.Comp.Mode = args.Mode;
        DirtyField(ent, ent.Comp, nameof(PlantAnalyzerComponent.Mode));
    }

    [SubscribeLocalEvent]
    private void OnSetGene(Entity<PlantAnalyzerComponent> ent, ref PlantAnalyzerSetGeneIndex args)
    {
        if (ent.Comp.Busy)
            return;

        var index = args.Index;
        var dirty = string.Empty;
        if (args.IsDatabank)
        {
            var len = ent.Comp.GeneBank.Count + ent.Comp.ConsumeGasesBank.Count + ent.Comp.ExudeGasesBank.Count + ent.Comp.ChemicalBank.Count;
            if (len == 0)
                return;
            ent.Comp.DatabankIndex = Math.Clamp(index, 0, len - 1);
            dirty = nameof(PlantAnalyzerComponent.DatabankIndex);
        }
        else
        {
            var len = SeedData.AllGenes.Length;
            if (len == 0)
                return;
            ent.Comp.GeneIndex = Math.Clamp(index, 0, len - 1);
            dirty = nameof(PlantAnalyzerComponent.GeneIndex);
        }
        DirtyField(ent, ent.Comp, dirty);
    }

    [SubscribeLocalEvent]
    public void OnDeleteDatabankEntry(Entity<PlantAnalyzerComponent> ent, ref PlantAnalyzerDeleteDatabankEntry args)
    {
        var index = ent.Comp.DatabankIndex;
        if (index < 0)
            return;

        if (index < ent.Comp.GeneBank.Count)
        {
            ent.Comp.GeneBank.RemoveAt(index);
            DirtyField(ent, ent.Comp, nameof(PlantAnalyzerComponent.GeneBank));
            return;
        }

        index -= ent.Comp.GeneBank.Count;
        if (index < ent.Comp.ConsumeGasesBank.Count)
        {
            ent.Comp.ConsumeGasesBank.RemoveAt(index);
            DirtyField(ent, ent.Comp, nameof(PlantAnalyzerComponent.ConsumeGasesBank));
            return;
        }

        index -= ent.Comp.ConsumeGasesBank.Count;
        if (index < ent.Comp.ExudeGasesBank.Count)
        {
            ent.Comp.ExudeGasesBank.RemoveAt(index);
            DirtyField(ent, ent.Comp, nameof(PlantAnalyzerComponent.ExudeGasesBank));
            return;
        }

        index -= ent.Comp.ExudeGasesBank.Count;
        if (index >= ent.Comp.ChemicalBank.Count)
            return;

        ent.Comp.ChemicalBank.RemoveAt(index);
        DirtyField(ent, ent.Comp, nameof(PlantAnalyzerComponent.ChemicalBank));
    }

    // This is some shit which is really fucking wack.
    public void GetGeneFromInteger(Entity<PlantAnalyzerComponent> ent, EntityUid target)
    {
        var (uid, seed) = GetPlantData(target);
        int index = ent.Comp.GeneIndex;
        if (index < 0 ||
            index >= SeedData.AllGenes.Length ||
            !_botany.TryGetPlantComponent<PlantComponent>(uid, seed, out var plant))
            return;

        var dirty = false;
        _botany.TryGetPlantComponent<PlantConsumeExudeGasComponent>(uid, seed, out var gases);
        switch (SeedData.AllGenes[index].Type)
        {
            case SeedDataType.Chemical:
                if (!_botany.TryGetPlantComponent<PlantChemicalsComponent>(uid, seed, out var chems))
                    return;

                foreach (var chemical in chems.Chemicals)
                {
                    var chem = new ChemData(chemical.Key, chemical.Value);
                    if (ent.Comp.ChemicalBank.Contains(chem))
                        continue;

                    ent.Comp.ChemicalBank.Add(chem);
                    dirty = true;
                }

                if (dirty)
                    DirtyField(ent, ent.Comp, nameof(PlantAnalyzerComponent.ChemicalBank));
                break;
            case SeedDataType.GasConsume:
                foreach (var (id, moles) in gases?.ConsumeGasses ?? [])
                {
                    var gas = new GasData(id, moles);
                    if (ent.Comp.ConsumeGasesBank.Contains(gas))
                        continue;

                    ent.Comp.ConsumeGasesBank.Add(gas);
                    dirty = true;
                }

                if (dirty)
                    DirtyField(ent, ent.Comp, nameof(PlantAnalyzerComponent.ConsumeGasesBank));
                break;
            case SeedDataType.GasExude:
                foreach (var (id, moles) in gases?.ExudeGasses ?? [])
                {
                    var gas = new GasData(id, moles);
                    if (ent.Comp.ConsumeGasesBank.Contains(gas))
                        continue;

                    ent.Comp.ExudeGasesBank.Add(gas);
                    dirty = true;
                }

                if (dirty)
                    DirtyField(ent, ent.Comp, nameof(PlantAnalyzerComponent.ExudeGasesBank));
                break;
            default:
                _botany.TryGetPlantComponent<PlantAtmosphericComponent>(uid, seed, out var atmos);
                _botany.TryGetPlantComponent<PlantGrowthComponent>(uid, seed, out var growth);
                _botany.TryGetPlantComponent<PlantHarvestComponent>(uid, seed, out var harvest);
                _botany.TryGetPlantComponent<PlantToxinsComponent>(uid, seed, out var toxins);
                _botany.TryGetPlantComponent<PlantWeedPestComponent>(uid, seed, out var weed);
                float? value = index switch
                {
                    0 => growth?.NutrientConsumption,
                    1 => growth?.WaterConsumption,
                    2 => toxins?.ToxinsTolerance,
                    3 => toxins?.ToxinUptakeDivisor,
                    4 => atmos?.LowHeatTolerance,
                    5 => atmos?.HighHeatTolerance,
                    6 => atmos?.LowPressureTolerance,
                    7 => atmos?.HighPressureTolerance,
                    8 => weed?.PestTolerance,
                    9 => weed?.WeedTolerance,
                    10 => plant.Endurance,
                    11 => plant.Lifespan,
                    12 => plant.Maturation,
                    13 => plant.Production,
                    14 => (float?) harvest?.HarvestRepeat,
                    15 => (float) plant.Yield,
                    16 => plant.Potency,
                    17 => _botany.PlantHasComp<PlantTraitSeedlessComponent>(uid, seed) ? 1f : 0f,
                    18 => _botany.PlantHasComp<PlantTraitUnviableComponent>(uid, seed) ? 1f : 0f,
                    19 => _botany.PlantHasComp<PlantTraitLigneousComponent>(uid, seed) ? 1f : 0f,
                    20 => _botany.PlantHasComp<PlantTraitScreamComponent>(uid, seed) ? 1f : 0f,
                    21 => _botany.PlantHasComp<PlantTraitKudzuComponent>(uid, seed) ? 1f : 0f,
                    _ => null
                };

                if (value == null)
                    break;

                var item = new GeneData(index, value.Value);
                if (ent.Comp.GeneBank.Contains(item))
                    break;

                ent.Comp.GeneBank.Add(item);
                DirtyField(ent, ent.Comp, nameof(PlantAnalyzerComponent.GeneBank));
                break;
        }
    }

    public void SetGeneFromInteger(Entity<PlantAnalyzerComponent> ent, Entity<SeedComponent> seed)
    {
        var uid = EnsurePlantData(ent, seed);

        if (!TryComp<PlantComponent>(uid, out var plant))
        {
            Log.Error($"{ToPrettyString(seed)} has invalid PlantData {ToPrettyString(uid)}!");
            return;
        }

        TryComp<PlantAtmosphericComponent>(uid, out var atmos);
        TryComp<PlantGrowthComponent>(uid, out var growth);
        TryComp<PlantHarvestComponent>(uid, out var harvest);
        TryComp<PlantToxinsComponent>(uid, out var toxins);
        TryComp<PlantWeedPestComponent>(uid, out var weed);
        var index = ent.Comp.DatabankIndex;
        if (index < ent.Comp.GeneBank.Count)
        {
            var gene = ent.Comp.GeneBank[index];
            var value = gene.GeneValue;
            switch (gene.GeneID)
            {
                case 0:
                    if (growth == null)
                        return;
                    growth.NutrientConsumption = value;
                    DirtyField(uid, growth, nameof(PlantGrowthComponent.NutrientConsumption));
                    break;
                case 1:
                    if (growth == null)
                        return;
                    growth.WaterConsumption = value;
                    DirtyField(uid, growth, nameof(PlantGrowthComponent.WaterConsumption));
                    break;
                case 2:
                    if (toxins == null)
                        return;
                    toxins.ToxinsTolerance = value;
                    DirtyField(uid, toxins, nameof(PlantToxinsComponent.ToxinsTolerance));
                    break;
                case 3:
                    if (toxins == null)
                        return;
                    toxins.ToxinUptakeDivisor = value;
                    DirtyField(uid, toxins, nameof(PlantToxinsComponent.ToxinUptakeDivisor));
                    break;
                case 4:
                    if (atmos == null)
                        return;
                    atmos.LowHeatTolerance = value;
                    DirtyField(uid, atmos, nameof(PlantAtmosphericComponent.LowHeatTolerance));
                    break;
                case 5:
                    if (atmos == null)
                        return;
                    atmos.HighHeatTolerance = value;
                    DirtyField(uid, atmos, nameof(PlantAtmosphericComponent.HighHeatTolerance));
                    break;
                case 6:
                    if (atmos == null)
                        return;
                    atmos.LowPressureTolerance = value;
                    DirtyField(uid, atmos, nameof(PlantAtmosphericComponent.LowPressureTolerance));
                    break;
                case 7:
                    if (atmos == null)
                        return;
                    atmos.HighPressureTolerance = value;
                    DirtyField(uid, atmos, nameof(PlantAtmosphericComponent.HighPressureTolerance));
                    break;
                case 8:
                    if (weed == null)
                        return;
                    weed.PestTolerance = value;
                    DirtyField(uid, weed, nameof(PlantWeedPestComponent.PestTolerance));
                    break;
                case 9:
                    if (weed == null)
                        return;
                    weed.WeedTolerance = value;
                    DirtyField(uid, weed, nameof(PlantWeedPestComponent.WeedTolerance));
                    break;
                case 10:
                    plant.Endurance = value;
                    DirtyField(uid, plant, nameof(PlantComponent.Endurance));
                    break;
                case 11:
                    plant.Lifespan = value;
                    DirtyField(uid, plant, nameof(PlantComponent.Lifespan));
                    break;
                case 12:
                    plant.Maturation = value;
                    DirtyField(uid, plant, nameof(PlantComponent.Maturation));
                    break;
                case 13:
                    plant.Production = value;
                    DirtyField(uid, plant, nameof(PlantComponent.Production));
                    break;
                case 14:
                    if (harvest == null)
                        return;
                    harvest.HarvestRepeat = (HarvestType) value;
                    DirtyField(uid, harvest, nameof(PlantHarvestComponent.HarvestRepeat));
                    break;
                case 15:
                    plant.Yield = (int) value;
                    DirtyField(uid, plant, nameof(PlantComponent.Yield));
                    break;
                case 16:
                    plant.Potency = value;
                    DirtyField(uid, plant, nameof(PlantComponent.Potency));
                    break;
                case 17:
                    SetTrait<PlantTraitSeedlessComponent>(uid, value);
                    break;
                case 18:
                    SetTrait<PlantTraitUnviableComponent>(uid, value);
                    break;
                case 19:
                    SetTrait<PlantTraitLigneousComponent>(uid, value);
                    break;
                case 20:
                    SetTrait<PlantTraitScreamComponent>(uid, value);
                    break;
                case 21:
                    SetTrait<PlantTraitKudzuComponent>(uid, value);
                    break;
            }
            return;
        }

        index -= ent.Comp.GeneBank.Count;
        if (index < ent.Comp.ConsumeGasesBank.Count)
        {
            if (!TryComp<PlantConsumeExudeGasComponent>(uid, out var gases))
                return;
            var gas = ent.Comp.ConsumeGasesBank[index];
            gases.ConsumeGasses[gas.GasID] = gas.GasValue;
            DirtyField(uid, gases, nameof(PlantConsumeExudeGasComponent.ConsumeGasses));
            return;
        }

        index -= ent.Comp.ConsumeGasesBank.Count;
        if (index < ent.Comp.ExudeGasesBank.Count)
        {
            if (!TryComp<PlantConsumeExudeGasComponent>(uid, out var gases))
                return;
            var gas = ent.Comp.ExudeGasesBank[index];
            gases.ExudeGasses[gas.GasID] = gas.GasValue;
            DirtyField(uid, gases, nameof(PlantConsumeExudeGasComponent.ExudeGasses));
            return;
        }

        index -= ent.Comp.ExudeGasesBank.Count;
        if (index >= ent.Comp.ChemicalBank.Count ||
            !TryComp<PlantChemicalsComponent>(uid, out var chems))
            return;

        var chem = ent.Comp.ChemicalBank[index];
        chems.Chemicals[chem.ChemID] = chem.ChemValue;
        DirtyField(uid, chems, nameof(PlantChemicalsComponent.Chemicals));
    }

    private void SetTrait<T>(EntityUid uid, float value)
    where
        T: IComponent, new()
    {
        if (value == 0f)
            RemComp<T>(uid);
        else
            EnsureComp<T>(uid);
    }
}
