// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.UserInterface.Controls;
using Content.Shared.Atmos;
using Content.Shared.Lock;
using Content.Trauma.Shared.Nuclear.Reactor;
using Robust.Shared.Timing;

namespace Content.Trauma.Client.Nuclear.Reactor.UI;

/// <summary>
/// Client-side UI used to view a nuclear reactor.
/// </summary>
[GenerateTypedNameReferences]
public sealed partial class NuclearReactorWindow : FancyWindow
{
    [Dependency] private IEntityManager _ent = default!;
    private readonly LockSystem _lock;
    private readonly EntityQuery<ReactorPartComponent> _partQuery;

    private readonly Dictionary<Vector2i, StyleBoxFlat> _reactorGrid = [];
    private readonly Dictionary<Vector2i, TextureRect> _reactorRect = [];
    private readonly Dictionary<Vector2i, Button> _reactorButton = [];

    private readonly StyleBoxFlat _temperatureBar = new(Color.Black);
    private readonly StyleBoxFlat _radiationBar = new(Color.Black);
    private readonly StyleBoxFlat _powerBar = new(Color.Black);

    private ReactorSlotBUIData[] _data = default!;
    private int _width = 0;
    private int _height = 0;

    private Entity<NuclearReactorComponent> _reactor = default!;
    private EntityUid? _monitor = default!;

    private DisplayModes _displayMode = DisplayModes.Temperature;

    private enum DisplayModes : byte
    {
        Temperature,
        Neutron,
        Target,
        Fuel
    }
    private int _displayModeCount = Enum.GetValues<DisplayModes>().Length;

    private bool _kelvin = true;

    private int _targetX = 0;
    private int _targetY = 0;
    private bool _hasTarget;
    private bool _hasItem;

    public event Action<Vector2i>? OnSwapPart;
    public event Action? OnEjectItem;
    public event Action<float>? OnAdjustControlRods;

    private readonly float _repeatDelay = 0.5f;
    private readonly Dictionary<Button, float> _repeatQueue = [];

    public NuclearReactorWindow()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        _lock = _ent.System<LockSystem>();
        _partQuery = _ent.GetEntityQuery<ReactorPartComponent>();

        ReactorTempBar.ForegroundStyleBoxOverride = _temperatureBar;
        ReactorRadsBar.ForegroundStyleBoxOverride = _radiationBar;
        ReactorThermBar.ForegroundStyleBoxOverride = _powerBar;

        ChangeViewButton.OnPressed += _ => OnChangeViewButtonPressed();
        XIncrement.OnPressed += _ => MoveTarget(1, 0);
        XDecrement.OnPressed += _ => MoveTarget(-1, 0);
        YIncrement.OnPressed += _ => MoveTarget(0, 1);
        YDecrement.OnPressed += _ => MoveTarget(0, -1);
        SwapPart.OnPressed += _ => OnSwapPart?.Invoke(new(_targetX, _targetY));
        EjectItem.OnPressed += _ => OnEjectItem?.Invoke();

        ControlRodsInsertLarge.OnPressed += _ => AdjustControlRods(0.1f);
        ControlRodsInsertLarge.OnButtonDown += _ => _repeatQueue.Add(ControlRodsInsertLarge, _repeatDelay);
        ControlRodsInsertLarge.OnButtonUp += _ => _repeatQueue.Remove(ControlRodsInsertLarge);

        ControlRodsInsert.OnPressed += _ => AdjustControlRods(0.01f);
        ControlRodsInsert.OnButtonDown += _ => _repeatQueue.Add(ControlRodsInsert, _repeatDelay);
        ControlRodsInsert.OnButtonUp += _ => _repeatQueue.Remove(ControlRodsInsert);

        ControlRodsRemove.OnPressed += _ => AdjustControlRods(-0.01f);
        ControlRodsRemove.OnButtonDown += _ => _repeatQueue.Add(ControlRodsRemove, _repeatDelay);
        ControlRodsRemove.OnButtonUp += _ => _repeatQueue.Remove(ControlRodsRemove);

        ControlRodsRemoveLarge.OnPressed += _ => AdjustControlRods(-0.1f);
        ControlRodsRemoveLarge.OnButtonDown += _ => _repeatQueue.Add(ControlRodsRemoveLarge, _repeatDelay);
        ControlRodsRemoveLarge.OnButtonUp += _ => _repeatQueue.Remove(ControlRodsRemoveLarge);

        TargetTemperature.OnPressed += _ => ChangeTemp();
    }

    public void Update(NuclearReactorBuiState msg)
    {
        _data = msg.SlotData;
    }

    private void Update()
    {
        var comp = _reactor.Comp;
        ReactorTempValue.Text = FormatTemperature(comp.Temperature);
        ReactorTempBar.Value = comp.Temperature;
        _temperatureBar.BackgroundColor = GetColor(Atmospherics.T20C, ReactorTempBar.MaxValue * 0.75, comp.Temperature);

        ReactorRadsValue.Text = comp.RadiationLevel <= comp.MaximumRadiation ? Math.Round(comp.RadiationLevel, 1).ToString() : "OVERLOAD";
        ReactorRadsBar.Value = comp.RadiationLevel;
        _radiationBar.BackgroundColor = GetColor(0, ReactorRadsBar.MaxValue * 0.5, comp.RadiationLevel);

        ReactorThermValue.Text = FormatPower(comp.ThermalPower);
        ReactorThermBar.Value = comp.ThermalPower;
        _powerBar.BackgroundColor = GetSteppedColor(ReactorThermBar.MaxValue * 0.75, ReactorThermBar.MaxValue, comp.ThermalPower);

        ControlRodsValue.Text = Math.Round(comp.AvgInsertion * 50, 1).ToString() + "%";
        ControlRodsActual.Value = comp.AvgInsertion;
        ControlRodsSet.Value = comp.ControlRodInsertion;

        var locktarget = _monitor ?? _reactor.Owner;
        var locked = _lock.IsLocked(_monitor ?? _reactor.Owner);

        ControlRodsButtons.Visible = !locked;
        SwapPart.Visible = !locked && _monitor == null;

        SwapPartLock.Visible = locked && _monitor == null;
        ControlRodsLock.Visible = locked;

        Shelf.Visible = _monitor == null;

        _hasItem = comp.PartSlot.Item != null;
        ItemName.Text = comp.PartSlot.Item is { } item ? Name(item) : "empty";
        EjectItem.Disabled = !_hasItem;
    }

    private new string Name(EntityUid uid)
        => _ent.GetComponent<MetaDataComponent>(uid).EntityName;

    public void SetEntity(Entity<NuclearReactorComponent> reactor, EntityUid? monitor = null)
    {
        _reactor = reactor;
        _monitor = monitor;

        this.SetInfoFromEntity(_ent, monitor ?? reactor.Owner);

        var comp = reactor.Comp;
        _width = comp.GridWidth;
        _height = comp.GridHeight;

        ReactorGrid.Columns = _width;

        ReactorTempBar.MaxValue = comp.ReactorMeltdownTemp;
        ReactorRadsBar.MaxValue = comp.MaximumRadiation;
        ReactorThermBar.MaxValue = comp.MaximumThermalPower;

        InitReactorGrid();
    }

    public void InitReactorGrid()
    {
        _data = new ReactorSlotBUIData[_width * _height];
        for (var y = 0; y < _height; y++)
        {
            for (var x = 0; x < _width; x++)
            {
                var styleBox = new StyleBoxFlat();
                var icon = new TextureRect()
                {
                    SetSize = new(32, 32),
                    TexturePath = "/Textures/_FarHorizons/Structures/Power/Generation/FissionGenerator/reactor_part_inserted/base.png"
                };
                var button = new Button
                {
                    Margin = new(0),
                    StyleBoxOverride = new StyleBoxFlat(Color.Transparent),
                    ToolTip = "",
                    TooltipDelay = 0.5f
                };

                var pos = new Vector2i(x, y);
                _reactorGrid.Add(pos, styleBox);
                _reactorRect.Add(pos, icon);
                _reactorButton.Add(pos, button);

                var control = new PanelContainer
                {
                    Margin = new Thickness(2),
                    PanelOverride = styleBox,
                    HorizontalExpand = true,
                    VerticalExpand = true,
                };
                control.AddChild(button);
                button.OnPressed += _ => SetTarget(pos);
                button.AddChild(icon);
                ReactorGrid.AddChild(control);
            }
        }
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        Update();

        //Update the grid
        for (var x = 0; x < _width; x++)
        {
            for (var y = 0; y < _height; y++)
            {
                var pos = new Vector2i(x, y);
                var index = x + y * _width;
                var data = _data[index];
                var box = _reactorGrid[pos];
                switch (_displayMode)
                {
                    case DisplayModes.Temperature:
                        box.BackgroundColor = GetColor(293.15, 1200, data?.Temperature ?? 0);
                        ViewLabel.Text = Loc.GetString("comp-nuclear-reactor-ui-view-temp");
                        break;
                    case DisplayModes.Neutron:
                        box.BackgroundColor = GetColor(0, 7, data?.NeutronCount ?? 0);
                        ViewLabel.Text = Loc.GetString("comp-nuclear-reactor-ui-view-neutron");
                        break;
                    case DisplayModes.Target:
                        box.BackgroundColor = x == _targetX && y == _targetY ? Color.Yellow : Color.Gray;
                        ViewLabel.Text = Loc.GetString("comp-nuclear-reactor-ui-view-target");
                        break;
                    case DisplayModes.Fuel:
                        box.BackgroundColor = Color.InterpolateBetween(Color.Gray, Color.FromHex("#22bbff"), data != null ? (float)GetFuelLevel(data) : 0);
                        ViewLabel.Text = Loc.GetString("comp-nuclear-reactor-ui-view-fuel");
                        break;
                }

                var part = _ent.GetEntity(_reactor.Comp.PartGrid[index]);
                var icon = _partQuery.TryComp(part, out var partComp) ? partComp.IconStateInserted : "base";
                _reactorRect[pos].TexturePath = $"/Textures/_FarHorizons/Structures/Power/Generation/FissionGenerator/reactor_part_inserted/{icon}.png";

                _reactorButton[pos].ToolTip = data != null
                    ? $"Fuel Level: {(int)Math.Round(GetFuelLevel(data) * 100)}%"
                    : "";
            }
        }

        UpdateTargetInfo();
        UpdateSwapPart();

        // Handle repeat buttons
        foreach (var (button, time) in _repeatQueue)
        {
            if (time > 0)
            {
                _repeatQueue[button] -= args.DeltaSeconds;
                continue;
            }

            _repeatQueue[button] = _repeatDelay;
            AdjustControlRods(button switch
            {
                var _ when button == ControlRodsInsertLarge => 0.1f,
                var _ when button == ControlRodsInsert => 0.01f,
                var _ when button == ControlRodsRemove => -0.01f,
                var _ when button == ControlRodsRemoveLarge => -0.1f,
                _ => 0f
            });
        }
    }

    private static Color GetColor(double pointA, double pointB, double value)
    {
        var mid = pointA+((pointB-pointA) / 2);
        Color result;

        // TODO: kys
        if (value < pointA && pointA > 0)
            result = Color.InterpolateBetween(Color.DarkBlue, Color.FromHex("#31843E"), (float)(value / pointA));
        else if (value >= pointA && value < mid)
            result = Color.InterpolateBetween(Color.FromHex("#31843E"), Color.FromHex("#BBBB00"), (float)((value - pointA) / (mid - pointA)));
        else if (value >= mid && value < pointB)
            result = Color.InterpolateBetween(Color.FromHex("#BBBB00"), Color.FromHex("#BB3232"), (float)((value - mid) / (pointB - mid)));
        else if (value >= pointB && value < pointB * 1.4)
            result = Color.InterpolateBetween(Color.FromHex("#BB3232"), Color.FromHex("#550000"), (float)((value - pointB) / ((pointB * 1.4) - pointB)));
        else if (value >= pointB * 1.4)
            result = Color.FromHex("#550000"); // Death.
        else
            result = Color.Black;

        return result;
    }

    private static Color _stepRed = Color.FromHex("#BB3232");
    private static Color _stepYellow = Color.FromHex("#BBBB00");
    private static Color _stepBlue = Color.FromHex("#31843E");
    private static Color GetSteppedColor(double pointA, double pointB, double value)
    {
        if (value > pointB)
            return _stepRed;
        if (value > pointA)
            return _stepYellow;
        return _stepBlue;
    }

    private void OnChangeViewButtonPressed()
    {
        var mode = (int) _displayMode + 1;
        mode %= _displayModeCount;
        _displayMode = (DisplayModes) mode;
    }

    private void ChangeTemp()
    {
        _kelvin = !_kelvin;
    }

    private string FormatTemperature(double temperature) => _kelvin
        ? Math.Round(temperature, 1).ToString() + " K"
        : Math.Round(temperature - Atmospherics.T0C, 1).ToString() + " C";

    private void UpdateTargetInfo()
    {
        var index = _targetX + _targetY * _width;
        var part = _ent.GetEntity(_reactor.Comp.PartGrid[index]);
        _hasTarget = part != null;
        TargetName.Text = part != null
            ? Name(part.Value)
            : "empty";
        var data = _data[index];
        if (data == null)
        {
            TargetTemperatureGrid.Visible = TargetNRadiationGrid.Visible = TargetRadiationGrid.Visible = TargetSpentGrid.Visible = false;
            return;
        }

        TargetTemperatureGrid.Visible = data.Temperature > 0;
        TargetTemperature.Text = FormatTemperature(data.Temperature);

        TargetNRadiationGrid.Visible = data.NeutronRadioactivity > 0;
        TargetNRadiation.Text = Math.Round(data.NeutronRadioactivity, 2).ToString();

        TargetRadiationGrid.Visible = data.Radioactivity > 0;
        TargetRadiation.Text = Math.Round(data.Radioactivity, 2).ToString();

        TargetSpentGrid.Visible = data.SpentFuel > 0;
        TargetSpent.Text = Math.Round(data.SpentFuel, 2).ToString();
    }

    private void UpdateSwapPart()
    {
        SwapPart.Disabled = _hasItem == _hasTarget;
        if (SwapPart.Disabled)
            return;

        SwapPart.Text = _hasTarget
            ? Loc.GetString("comp-nuclear-reactor-ui-remove-button")
            : Loc.GetString("comp-nuclear-reactor-ui-insert-button");
    }

    private void MoveTarget(int x, int y) => SetTarget(_targetX + x, _targetY + y);

    private void SetTarget(Vector2i pos) => SetTarget(pos.X, pos.Y);

    private void SetTarget(int x, int y)
    {
        _targetX = Math.Clamp(x, 0, _width - 1);
        _targetY = Math.Clamp(y, 0, _height - 1);

        TargetPos.Text = $"{_targetX},{_targetY}";

        XIncrement.Disabled = _targetX >= _width - 1;
        XDecrement.Disabled = _targetX <= 0;
        YIncrement.Disabled = _targetY >= _height - 1;
        YDecrement.Disabled = _targetY <= 0;
    }

    private static string FormatPower(float power)
        => Loc.GetString("comp-nuclear-reactor-ui-therm-format", ("power", power));

    private static double GetFuelLevel(ReactorSlotBUIData data)
        => Math.Max(1 - (data.SpentFuel / (data.SpentFuel + (data.Radioactivity * 0.5) + (data.NeutronRadioactivity * 0.25))), 0);

    private void AdjustControlRods(float change)
    {
        OnAdjustControlRods?.Invoke(change);
    }
}
