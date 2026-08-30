// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.UserInterface.Controls;
using Content.Shared.Atmos;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Items.Components;
using Content.Shared.Botany.Systems;
using Content.Shared.Random;
using Content.Trauma.Shared.Botany.PlantAnalyzer;
using Robust.Shared.Timing;
using System.Text;

namespace Content.Trauma.Client.Botany.PlantAnalyzer.UI;

[GenerateTypedNameReferences]
public sealed partial class PlantAnalyzerWindow : FancyWindow
{
    [Dependency] private IEntityManager _ent = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    private BotanySystem _botany = default!;
    private SharedAtmosphereSystem _atmos = default!;

    public event Action<PlantAnalyzerModes>? OnSetMode;
    public event Action<int>? OnSelectGene;
    public event Action<int>? OnSelectEntry;
    public event Action? OnDeleteEntry;

    private PlantAnalyzerComponent _comp = default!;
    private EntityUid? _plant;
    private EntProtoId? _seed;
    private int _geneCount = -1;
    private int _consumeCount = -1;
    private int _exudeCount = -1;
    private int _chemicalCount = -1;
    private bool _updating;

    private const string IndentedNewline = "\n   ";

    public PlantAnalyzerWindow()
    {
        IoCManager.InjectDependencies(this);
        RobustXamlLoader.Load(this);

        _botany = _ent.System<BotanySystem>();
        _atmos = _ent.System<SharedAtmosphereSystem>();

        Tabs.OnTabChanged += _ =>
        {
            OnSetMode?.Invoke((PlantAnalyzerModes) Tabs.CurrentTab);
        };

        // offset individual lists index to shitty per-databank index
        GeneDatabaseList.OnItemSelected += args => SelectEntry(args.ItemIndex, 0);
        ConsumeDatabaseList.OnItemSelected += args => SelectEntry(args.ItemIndex, _geneCount);
        ExudeDatabaseList.OnItemSelected += args => SelectEntry(args.ItemIndex, _geneCount + _consumeCount);
        ChemicalDatabaseList.OnItemSelected += args => SelectEntry(args.ItemIndex, _geneCount + _consumeCount + _exudeCount);

        DeleteDatabaseEntryButton.OnPressed += _ => OnDeleteEntry?.Invoke();
    }

    private void SelectEntry(int i, int offset)
    {
        if (!_updating)
            OnSelectEntry?.Invoke(i + offset);
    }

    public void SetOwner(EntityUid uid)
    {
        _comp = _ent.GetComponent<PlantAnalyzerComponent>(uid);
        Tabs.CurrentTab = (int) _comp.Mode;

        GeneList.Clear();
        foreach (var entry in SeedData.AllGenes)
        {
            GeneList.AddItem(entry.Name);
        }
        GeneList[_comp.GeneIndex].Selected = true;
        GeneList.OnItemSelected += args => OnSelectGene?.Invoke(args.ItemIndex);

        Update();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        Update();
    }

    private void Update()
    {
        _updating = true;
        if (_comp.GeneBank.Count != _geneCount)
        {
            _geneCount = _comp.GeneBank.Count;
            UpdateGenes();
        }
        if (_comp.ConsumeGasesBank.Count != _consumeCount)
        {
            _consumeCount = _comp.ConsumeGasesBank.Count;
            UpdateGases(ConsumeDatabaseList, _comp.ConsumeGasesBank, "Consume", 0);
        }
        if (_comp.ExudeGasesBank.Count != _exudeCount)
        {
            _exudeCount = _comp.ExudeGasesBank.Count;
            UpdateGases(ExudeDatabaseList, _comp.ExudeGasesBank, "Exude", _consumeCount);
        }
        if (_comp.ChemicalBank.Count != _chemicalCount)
        {
            _chemicalCount = _comp.ChemicalBank.Count;
            UpdateChemicals();
        }

        if (_comp.Plant != _plant || _comp.Seed != _seed)
        {
            _plant = _comp.Plant;
            _seed = _comp.Seed;
            Populate(_plant, _seed);
        }
        _updating = false;
    }

    private void UpdateGenes()
    {
        GeneDatabaseList.Clear();
        foreach (var gene in _comp.GeneBank)
        {
            var entry = SeedData.AllGenes[gene.GeneID];
            var mutationValue = entry.Type switch
            {
                SeedDataType.Float => $"{gene.GeneValue:F2}",
                SeedDataType.Int => $"{(int) gene.GeneValue:D0}",
                SeedDataType.HarvestType => Loc.GetString($"plant-analyzer-harvest-{((HarvestType) gene.GeneValue).ToString()}"),
                SeedDataType.Bool => gene.GeneValue == 0f ? "false" : "true",
                _ => "N/A"
            };
            GeneDatabaseList.AddItem($"{entry.Name}: {mutationValue}");
        }

        if (_comp.DatabankIndex < _geneCount)
            GeneDatabaseList[_comp.DatabankIndex].Selected = true;
    }

    private void UpdateGases(ItemList list, List<GasData> databank, string type, int offset)
    {
        list.Clear();
        foreach (var gene in databank)
        {
            list.AddItem($"{type} {GasName(gene.GasID)}: {gene.GasValue}");
        }

        var len = databank.Count;
        var index = _comp.DatabankIndex - (_geneCount + offset);
        if (index >= 0 && index < len)
            list[index].Selected = true;
    }

    private void UpdateChemicals()
    {
        ChemicalDatabaseList.Clear();
        foreach (var gene in _comp.ChemicalBank)
        {
            var data = gene.ChemValue;
            var foreign = data.Inherent ? "" : " [Foreign]";
            ChemicalDatabaseList.AddItem($"{gene.ChemID}{foreign}: Min - {data.Min}, Max - {data.Max}, Potency Divisor - {data.PotencyDivisor}");
        }

        var index = _comp.DatabankIndex - (_geneCount + _consumeCount + _exudeCount);
        if (index >= 0 && index < _chemicalCount)
            ChemicalDatabaseList[index].Selected = true;
    }

    public void Populate(EntityUid? uid, EntProtoId? seed)
    {
        NoData.Visible = uid == null && seed == null;
        if (NoData.Visible)
            return;

        if (!_botany.TryGetPlantComponent<PlantComponent>(uid, seed, out var plant) ||
            !_botany.TryGetPlantComponent<PlantDataComponent>(uid, seed, out var data))
            return;

        // Process message fields into strings.
        StringBuilder chemString = new();
        if (_botany.TryGetPlantComponent<PlantChemicalsComponent>(uid, seed, out var chems))
        {
            foreach (var chem in chems.Chemicals.Keys)
            {
                chemString.Append(IndentedNewline);
                chemString.Append(_proto.Index(chem).LocalizedName);
            }
        }

        StringBuilder exudeGases = new();
        StringBuilder consumeGases = new();
        if (_botany.TryGetPlantComponent<PlantConsumeExudeGasComponent>(uid, seed, out var gases))
        {
            foreach (var gas in gases.ExudeGasses.Keys)
            {
                exudeGases.Append(IndentedNewline);
                exudeGases.Append(GasName(gas));
            }

            foreach (var gas in gases.ConsumeGasses.Keys)
            {
                consumeGases.Append(IndentedNewline);
                consumeGases.Append(GasName(gas));
            }
        }

        _botany.TryGetPlantComponent<PlantAtmosphericComponent>(uid, seed, out var atmos);
        _botany.TryGetPlantComponent<PlantGrowthComponent>(uid, seed, out var growth);
        _botany.TryGetPlantComponent<PlantHarvestComponent>(uid, seed, out var harvest);
        _botany.TryGetPlantComponent<PlantToxinsComponent>(uid, seed, out var toxins);
        _botany.TryGetPlantComponent<PlantWeedPestComponent>(uid, seed, out var weeds);

        PlantName.Text = seed is { } id
            ? $"Scanned seed: {_proto.Index(id).Name}"
            : $"Scanned plant: {Name(uid)}";

        // Basics
        PlantYield.Text = $"Yield: {plant.Yield}";
        Potency.Text = $"Potency: {plant.Potency:F0}%";
        var harvestType = harvest?.HarvestRepeat is { } repeat
            ? Loc.GetString($"plant-analyzer-harvest-{repeat}")
            : "N/A";
        Repeat.Text = $"Harvest type: {harvestType}";
        Endurance.Text = $"Endurance: {plant.Endurance:F0}";
        Chemicals.Text = $"Contained substances: {chemString}";
        var exudeText = exudeGases.Length == 0 ? "None" : exudeGases.ToString();
        ExudeGases.Text = $"Emitted gases: {exudeText}";
        var consumeText = consumeGases.Length == 0 ? "None" : consumeGases.ToString();
        ConsumeGases.Text = $"Consumed gases: {consumeText}";
        Lifespan.Text = $"Lifespan: {plant.Lifespan:F1}";
        Maturation.Text = $"Maturation: {plant.Maturation:F1}";
        Production.Text = $"Production: {plant.Production:F1}";
        GrowthStages.Text = $"Growth stages: {plant.GrowthStages}";
        // Tolerances
        NutrientUsage.Text = $"Nutrient usage: {growth?.NutrientConsumption ?? 0f:F2}";
        WaterUsage.Text = $"Water usage: {growth?.WaterConsumption ?? 0f:F2}";
        ToxinsTolerance.Text = $"Toxins tolerance: {toxins?.ToxinsTolerance:F1}";
        ToxinUptakeDivisor.Text = $"Toxins resistance: {toxins?.ToxinUptakeDivisor:F1}";
        LowHeatTolerance.Text = $"Cold tolerance: {atmos?.LowHeatTolerance:F1} K";
        HighHeatTolerance.Text = $"Heat tolerance: {atmos?.HighHeatTolerance:F1} K";
        LowPressureTolerance.Text = $"Low pressure tolerance: {atmos?.LowPressureTolerance} kPa";
        HighPressureTolerance.Text = $"High pressure tolerance: {atmos?.HighPressureTolerance} kPa";
        PestTolerance.Text = $"Pest tolerance: {weeds?.PestTolerance:F1}";
        WeedTolerance.Text = $"Weed tolerance: {weeds?.WeedTolerance:F1}";

        // Misc
        StringBuilder mutations = new();
        if (_comp.ScannedMutations.Count == 0)
        {
            mutations.Append('-');
        }
        else
        {
            foreach (var mutation in _comp.ScannedMutations)
            {
                mutations.Append(IndentedNewline);
                mutations.Append(mutation);
            }
        }

        Traits.Text = $"Mutations: {mutations}";

        StringBuilder speciation = new();
        if (data.MutationPrototypes.Count == 0)
        {
            speciation.Append('-');
        }
        else
        {
            foreach (var mutation in data.MutationPrototypes)
            {
                speciation.Append(IndentedNewline);
                speciation.Append(_proto.Index(mutation).Name);
            }
        }

        PlantMutations.Text = $"Possible subtypes: {speciation}";
    }

    private new string Name(EntityUid? uid)
        => _ent.GetComponentOrNull<MetaDataComponent>(uid)?.EntityName ?? string.Empty;

    private string GasName(Gas gas)
        => Loc.GetString(_atmos.GetGas(gas).Name);
}
