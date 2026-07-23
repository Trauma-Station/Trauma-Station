// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using Content.Client.UserInterface.Controls;
using Content.Shared.IdentityManagement;
using Content.Shared.Lock;
using Content.Shared.Rounding;
using Content.Trauma.Shared.Nuclear.Turbine;
using Robust.Shared.Timing;

namespace Content.Trauma.Client.Nuclear.Turbine.UI;

/// <summary>
/// Client-side UI used to control a turbine.
/// </summary>
[GenerateTypedNameReferences]
public sealed partial class TurbineWindow : FancyWindow
{
    // Dependencies
    [Dependency] private IEntityManager _ent = default!;
    private readonly LockSystem _lock;

    #region Variables
    //Colors for the RPM meter. (lit)
    private static readonly Color[] _speedColors = [
        Color.FromHex("#BB3232"),
        Color.FromHex("#BB3232"),
        Color.FromHex("#BB3232"),
        Color.FromHex("#C49438"),
        Color.FromHex("#C49438"),
        Color.FromHex("#C49438"),
        Color.FromHex("#B3BF28"),
        Color.FromHex("#B3BF28"),
        Color.FromHex("#B3BF28"),
        Color.FromHex("#6FC938"),
        Color.FromHex("#C49438"),
        Color.FromHex("#BB3232"),
    ];

    //Colors for the RPM meter. (unlit)
    private static readonly Color[] _speedColorsDim = new Color[12];

    // Style boxes for the RPM meter.
    private StyleBoxFlat[] _speedMeter;

    private int _speedLevel;
    private float _flowRate = -1;
    private float _statorLoad = -1;
    private int _bladeHealth = -1;
    private int _rpmPercent = -1;
    private float _powerGen = -1;
    private float _powerSupply = -1;
    private EntityUid? _blade;
    private EntityUid? _stator;

    private Entity<TurbineComponent> _turbine = default!;
    private EntityUid? _monitor;

    private bool _suppressSliderEvents;
    private bool _suppressStatorUpdate;
    private bool _suppressFlowUpdate;

    private readonly float _repeatDelay = 0.5f;
    private readonly Dictionary<Button, float> _repeatQueue = [];
    #endregion

    #region Events
    public event Action<float>? OnChangeFlowRate;
    public event Action<float>? OnChangeStatorLoad;
    #endregion

    public TurbineWindow()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        _lock = _ent.System<LockSystem>();

        InitRPMMeter();

        // Handle flow rate
        TurbineFlowRateLabel.OnFocusEnter += _ => _suppressFlowUpdate = true;
        TurbineFlowRateLabel.OnFocusExit += _ => FlowTextChanged();
        TurbineFlowRateLabel.OnTextEntered += _ => FlowTextChanged(true);

        TurbineFlowRateSlider.OnValueChanged += _ =>
        {
            if (!_suppressSliderEvents)
                OnChangeFlowRate?.Invoke(TurbineFlowRateSlider.Value);
        };
        FlowRateDecrease.OnPressed += _ => OnChangeFlowRate?.Invoke(_flowRate - 100);
        FlowRateDecrease.OnButtonDown += _ => _repeatQueue.Add(FlowRateDecrease, _repeatDelay);
        FlowRateDecrease.OnButtonUp += _ => _repeatQueue.Remove(FlowRateDecrease);

        FlowRateIncrease.OnPressed += _ => OnChangeFlowRate?.Invoke(_flowRate + 100);
        FlowRateIncrease.OnButtonDown += _ => _repeatQueue.Add(FlowRateIncrease, _repeatDelay);
        FlowRateIncrease.OnButtonUp += _ => _repeatQueue.Remove(FlowRateIncrease);

        // Handle stator load
        TurbineStatorLoadLabel.OnFocusEnter += _ => _suppressStatorUpdate = true;
        TurbineStatorLoadLabel.OnFocusExit += _ => StatorTextChanged();
        TurbineStatorLoadLabel.OnTextEntered += _ => StatorTextChanged(true);

        StatorLoadDecreaseLarge.OnPressed += _ => OnChangeStatorLoad?.Invoke(_statorLoad - 1000);
        StatorLoadDecreaseLarge.OnButtonDown += _ => _repeatQueue.Add(StatorLoadDecreaseLarge, _repeatDelay);
        StatorLoadDecreaseLarge.OnButtonUp += _ => _repeatQueue.Remove(StatorLoadDecreaseLarge);

        StatorLoadDecrease.OnPressed += _ => OnChangeStatorLoad?.Invoke(_statorLoad - 100);
        StatorLoadDecrease.OnButtonDown += _ => _repeatQueue.Add(StatorLoadDecrease, _repeatDelay);
        StatorLoadDecrease.OnButtonUp += _ => _repeatQueue.Remove(StatorLoadDecrease);

        StatorLoadIncrease.OnPressed += _ => OnChangeStatorLoad?.Invoke(_statorLoad + 100);
        StatorLoadIncrease.OnButtonDown += _ => _repeatQueue.Add(StatorLoadIncrease, _repeatDelay);
        StatorLoadIncrease.OnButtonUp += _ => _repeatQueue.Remove(StatorLoadIncrease);

        StatorLoadIncreaseLarge.OnPressed += _ => OnChangeStatorLoad?.Invoke(_statorLoad + 1000);
        StatorLoadIncreaseLarge.OnButtonDown += _ => _repeatQueue.Add(StatorLoadIncreaseLarge, _repeatDelay);
        StatorLoadIncreaseLarge.OnButtonUp += _ => _repeatQueue.Remove(StatorLoadIncreaseLarge);

        CTabContainer.SetTabTitle(0, Loc.GetString("comp-turbine-ui-tab-main"));
        CTabContainer.SetTabTitle(1, Loc.GetString("comp-turbine-ui-tab-parts"));

        return;

        void FlowTextChanged(bool suppress = false)
        {
            if (float.TryParse(TurbineFlowRateLabel.Text, out var num) && !float.IsNaN(num))
                OnChangeFlowRate?.Invoke(num);
            _suppressFlowUpdate = suppress;
        }

        void StatorTextChanged(bool suppress = false)
        {
            if (float.TryParse(TurbineStatorLoadLabel.Text, out var num) && !float.IsNaN(num))
                OnChangeStatorLoad?.Invoke(num);
            _suppressStatorUpdate = suppress;
        }
    }

    #region Graphics
    public void SetEntity(Entity<TurbineComponent> turbine, EntityUid? monitor = null)
    {
        _turbine = turbine;
        _monitor = monitor;

        this.SetInfoFromEntity(_ent, monitor ?? turbine.Owner);

        EntityView.SetEntity(turbine);
        Update();
    }

    [MemberNotNull(nameof(_speedMeter))]
    public void InitRPMMeter()
    {
        _speedMeter = new StyleBoxFlat[_speedColors.Length];

        for (var i = _speedColors.Length - 1; i >= 0; i--)
        {
            _speedColorsDim[i] = DimGaugeColor(_speedColors[i]);
            var styleBox = new StyleBoxFlat();
            _speedMeter[i] = styleBox;

            for (var j = 0; j < RPMMeter.Columns; j++)
            {
                var control = new PanelContainer
                {
                    Margin = new Thickness(2),
                    PanelOverride = styleBox,
                    HorizontalExpand = true,
                    VerticalExpand = true,
                };
                RPMMeter.AddChild(control);
            }
        }
    }

    private static Color DimGaugeColor(Color color)
    {
        var hsv = Color.ToHsv(color);
        hsv.Z /= 5;
        return Color.FromHsv(hsv);
    }

    public void Update()
    {
        var comp = _turbine.Comp;
        UpdateIndicators(comp);

        if (comp.FlowRate != _flowRate)
        {
            _flowRate = comp.FlowRate;
            if (!_suppressFlowUpdate)
                TurbineFlowRateLabel.Text = Math.Round(_flowRate).ToString();
        }
        if (comp.StatorLoad != _statorLoad)
        {
            _statorLoad = comp.StatorLoad;
            if (!_suppressStatorUpdate)
                TurbineStatorLoadLabel.Text = Math.Round(_statorLoad).ToString();
        }

        var locktarget = _monitor ?? _turbine.Owner;
        Inputs.Visible = !_lock.IsLocked(locktarget);
        LockedMessage.Visible = _lock.IsLocked(locktarget);

        _suppressSliderEvents = true;
        TurbineFlowRateSlider.MaxValue = comp.FlowRateMax;
        TurbineFlowRateSlider.Value = comp.FlowRate;
        _suppressSliderEvents = false;

        _speedLevel = ContentHelpers.RoundToNearestLevels(comp.RPM, comp.BestRPM * 1.2, _speedMeter.Length);

        if (comp.CurrentBlade != _blade)
        {
            _blade = comp.CurrentBlade;
            if (_blade is { } blade)
            {
                BladeEntityView.SetEntity(blade);
                BladeInfoName.Text = Name(blade);
            }

            BladeInfo.Visible = _blade != null;
            _bladeHealth = -1; // just incase BladeHealthMax changed
        }
        if (comp.BladeHealth != _bladeHealth)
        {
            _bladeHealth = comp.BladeHealth;
            var percent = Math.Round(_bladeHealth * 100.0 / comp.BladeHealthMax);
            BladeInfoIntegrity.Text = $"{percent}%";
        }
        var rpmPercent = (int) Math.Round(comp.RPM * 100f / (comp.BestRPM * 1.2));
        if (rpmPercent != _rpmPercent)
        {
            _rpmPercent = rpmPercent;
            BladeInfoStress.Text = $"{rpmPercent}%";
        }

        if (comp.CurrentStator != _stator)
        {
            _stator = comp.CurrentStator;
            if (_stator is { } stator)
            {
                StatorEntityView.SetEntity(stator);
                StatorInfoName.Text = Name(stator);
            }

            StatorInfo.Visible = _stator != null;
        }

        if (comp.LastGen != _powerGen)
        {
            _powerGen = comp.LastGen;
            StatorInfoPotential.Text = Loc.GetString("comp-turbine-ui-power", ("power", _powerGen));
        }
        if (comp.PowerSupply != _powerSupply)
        {
            _powerSupply = comp.PowerSupply;
            StatorInfoSupply.Text = Loc.GetString("comp-turbine-ui-power", ("power", _powerSupply));
        }
    }

    private new string Name(EntityUid uid)
        => _ent.GetComponent<MetaDataComponent>(uid).EntityName;

    private void UpdateIndicators(TurbineComponent comp)
    {
        string Lit(bool b)
            => b ? "lit.png" : "dim.png";

        var root = "/Textures/_FarHorizons/Structures/Power/Generation/FissionGenerator/indicator_lamps/";
        TurbineOverspeed.TexturePath = $"{root}red{Lit(comp.Overspeed)}";
        TurbineOvertemp.TexturePath = $"{root}red{Lit(comp.Overtemp)}";
        TurbineStalling.TexturePath = $"{root}amber{Lit(comp.Stalling)}";
        TurbineUndertemp.TexturePath = $"{root}blue{Lit(comp.Undertemp)}";
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        Update();

        for (var i = 0; i < _speedMeter.Length; i++)
        {
            var box = _speedMeter[i];
            box.BackgroundColor = (_speedLevel > i ? _speedColors : _speedColorsDim)[i];
        }

        foreach (var (button, time) in _repeatQueue)
        {
            if (time > 0)
            {
                _repeatQueue[button] -= args.DeltaSeconds;
                continue;
            }

            _repeatQueue[button] = _repeatDelay;

            // TODO: something better holy shit
            switch (button)
            {
                case var _ when button == FlowRateDecrease:
                    OnChangeFlowRate?.Invoke(_flowRate - 100);
                    break;
                case var _ when button == FlowRateIncrease:
                    OnChangeFlowRate?.Invoke(_flowRate + 100);
                    break;
                case var _ when button == StatorLoadDecreaseLarge:
                    OnChangeStatorLoad?.Invoke(_statorLoad - 1000);
                    break;
                case var _ when button == StatorLoadDecrease:
                    OnChangeStatorLoad?.Invoke(_statorLoad - 100);
                    break;
                case var _ when button == StatorLoadIncrease:
                    OnChangeStatorLoad?.Invoke(_statorLoad + 100);
                    break;
                case var _ when button == StatorLoadIncreaseLarge:
                    OnChangeStatorLoad?.Invoke(_statorLoad + 1000);
                    break;
            }
        }
    }
    #endregion
}
