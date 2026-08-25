// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Goobstation.Shared.Chemistry;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Timing;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Goobstation.Client.Chemistry.UI;

[GenerateTypedNameReferences]
public sealed partial class EnergyReagentDispenserWindow : FancyWindow
{
    [Dependency] private IEntityManager _ent = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    private SharedBatterySystem _battery = default!;
    private SharedSolutionContainerSystem _solution = default!;

    public event Action? OnEjectBeaker;
    public event Action? OnClearBeaker;
    public event Action<int>? OnSetAmount;
    public event Action<ProtoId<ReagentPrototype>>? OnDispenseReagent;

    private EnergyReagentDispenserComponent _comp = default!;
    private Entity<BatteryComponent?> _batteryEnt = default!;
    private EntityUid? _beaker;
    private FixedPoint2 _lastVolume = -1;
    private float _batteryCharge = -1;
    private int _selectedAmount = -1;
    private float _lastBatteryCharge = -1;

    public EnergyReagentDispenserWindow()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        _battery = _ent.System<SharedBatterySystem>();
        _solution = _ent.System<SharedSolutionContainerSystem>();

        EjectButton.OnPressed += _ => OnEjectBeaker?.Invoke();
        ClearButton.OnPressed += _ => OnClearBeaker?.Invoke();
        AmountGrid.OnButtonPressed += s => OnSetAmount?.Invoke(int.Parse(s));
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        Update();
    }

    public void SetOwner(EntityUid uid, EnergyReagentDispenserComponent comp)
    {
        _comp = comp;
        _batteryEnt = (uid, _ent.GetComponent<BatteryComponent>(uid));

        SetupReagentsList();
        Update();
    }

    private void SetupReagentsList()
    {
        ReagentList.Children.Clear();

        foreach (var (id, cost) in _comp.Reagents)
        {
            var card = new EnergyReagentCardControl(_proto.Index(id), cost);
            card.OnPressed += id => OnDispenseReagent?.Invoke(id);
            ReagentList.Children.Add(card);
        }

        UpdateCardStates();
    }

    private void UpdateBatteryPercent()
    {
        var max = _batteryEnt.Comp.MaxCharge;
        var batteryPercent = max > 0
            ? _batteryCharge * 100 / max
            : 0;

        BatteryStatusLabel.Text = $"{_batteryCharge,3:F0}/{max,3:F0} ({batteryPercent,3:F0}%)";
        BatteryStatusLabel.StyleClasses.Clear();
        BatteryStatusLabel.StyleClasses.Add(batteryPercent switch
        {
            > 60 => "Good",
            > 30 => "Caution",
            _ => "Danger",
        });
    }

    private void BeakerChanged()
    {
        View.SetEntity(_beaker);

        var empty = _beaker == null;
        NoContainer.Visible = empty;
        ClearButton.Disabled = empty;
        EjectButton.Disabled = empty;
        ContainerReagents.Visible = !empty;
        if (_beaker is not { } beaker)
        {
            ContainerInfoName.Text = string.Empty;
            ContainerInfoFill.Text = string.Empty;
            ContainerReagents.RemoveAllChildren();
            return;
        }

        if (_ent.TryGetComponent<MetaDataComponent>(beaker, out var meta))
            ContainerInfoName.Text = meta.EntityName;
    }

    private void UpdateContents(EntityUid beaker)
    {
        if (!_solution.TryGetFitsInDispenser(beaker, out _, out var sol))
            return; // bug with the item slot whitelist if this happens

        // currently there should never be a way to have the reagents change without the volume changing
        // so this is cheap change detection
        OnChanged(ref _lastVolume, sol.Volume, () => SolutionChanged(sol));
    }

    private void SolutionChanged(Solution sol)
    {
        ContainerInfoFill.Text = $"{sol.Volume} / {sol.MaxVolume}";

        ContainerReagents.RemoveAllChildren();
        foreach (var pair in sol.Contents)
        {
            var id = pair.Reagent.Prototype;
            var localizedName = _proto.Resolve(id, out var proto)
                ? proto.LocalizedName
                : $"??? {id} ???";

            var nameLabel = new Label { Text = $"{localizedName}: " };
            var quantityLabel = new Label
            {
                Text = Loc.GetString("reagent-dispenser-window-quantity-label-text", ("quantity", pair.Quantity)),
                StyleClasses = { StyleNano.StyleClassLabelSecondaryColor },
            };

            ContainerReagents.Children.Add(new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                Children =
                {
                    nameLabel,
                    quantityLabel
                }
            });
        }
    }

    delegate void Changed();

    private void OnChanged<T>(ref T cached, T value, Changed changed)
    where
        T: notnull, IEquatable<T>
    {
        if (cached.Equals(value))
            return;

        cached = value;
        changed();
    }

    private void OnChanged<T>(ref T? cached, T? value, Changed changed)
    where
        T: struct, IEquatable<T>
    {
        if (EqualityComparer<T?>.Default.Equals(cached, value))
            return;

        cached = value;
        changed();
    }

    private void Update()
    {
        OnChanged(ref _selectedAmount, _comp.DispenseAmount, () =>
        {
            AmountGrid.Selected = _selectedAmount.ToString();
            UpdateCardStates();
        });

        OnChanged(ref _beaker, _comp.Beaker, BeakerChanged);
        if (_beaker is { } beaker)
            UpdateContents(beaker);

        var oldCharge = _batteryCharge;
        _batteryCharge = _battery.GetCharge(_batteryEnt);

        // dont care if 1000J becomes 1000.1
        if ((int) oldCharge != (int) _batteryCharge)
        {
            UpdateBatteryPercent();
            UpdateCardStates();
        }
    }

    private void UpdateCardStates()
    {
        foreach (var child in ReagentList.Children)
        {
            if (child is not EnergyReagentCardControl card)
                continue;

            var totalCost = card.EnergyCost * _selectedAmount;
            card.SetAmount(_selectedAmount);
            card.SetDisabled(totalCost > _batteryCharge, "Insufficient energy");
        }
    }
}
