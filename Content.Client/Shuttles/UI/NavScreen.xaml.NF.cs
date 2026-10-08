// New Frontiers - This file is licensed under AGPLv3
// Copyright (c) 2024 New Frontiers Contributors
// See AGPLv3.txt for details.

using Content.Trauma.Common.Shuttles;
using Content.Shared.DeviceLinking;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client.Shuttles.UI;

public sealed partial class NavScreen
{
    [Dependency] private IPrototypeManager _proto = default!;

    private readonly ButtonGroup _buttonGroup = new();
    public event Action<NetEntity?, InertiaDampeningMode>? OnInertiaDampeningModeChanged;

    public event Action<string>? OnNetworkPortButtonPressed;

    private void InitTrauma()
    {
        IFFShuttleToggle.OnToggled += OnIFFShuttleTogglePressed;
        IFFShuttleToggle.Pressed = NavRadar.ShowIFFShuttles;

        // IFF search
        IffSearchCriteria.OnTextChanged += args => OnIffSearchChanged(args.Text);

        // Maximum IFF Distance
        MaximumIFFDistanceValue.GetChild(0).GetChild(1).Margin = new Thickness(8, 0, 0, 0);
        MaximumIFFDistanceValue.OnValueChanged += args => OnRangeFilterChanged(args);

        DampenerOff.OnPressed += _ => SetDampenerMode(InertiaDampeningMode.Cruise);
        DampenerOn.OnPressed += _ => SetDampenerMode(InertiaDampeningMode.Dampen);
        AnchorOn.OnPressed += _ => SetDampenerMode(InertiaDampeningMode.Anchor);

        DampenerOff.Group = _buttonGroup;
        DampenerOn.Group = _buttonGroup;
        AnchorOn.Group = _buttonGroup;

        // Network Port Buttons
        DeviceButton1.OnPressed += _ => OnPortButtonPressed("SignalShuttleConsole1");
        DeviceButton2.OnPressed += _ => OnPortButtonPressed("SignalShuttleConsole2");
        DeviceButton3.OnPressed += _ => OnPortButtonPressed("SignalShuttleConsole3");
        DeviceButton4.OnPressed += _ => OnPortButtonPressed("SignalShuttleConsole4");

        // Send off a request to get the current dampening mode.
        _entManager.TryGetNetEntity(_shuttleEntity, out var shuttle);
        OnInertiaDampeningModeChanged?.Invoke(shuttle, InertiaDampeningMode.None);
    }

    private void OnPortButtonPressed(string sourcePort)
    {
        OnNetworkPortButtonPressed?.Invoke(sourcePort);
    }

    private void SetDampenerMode(InertiaDampeningMode mode)
    {
        NavRadar.DampeningMode = mode;
        _entManager.TryGetNetEntity(_shuttleEntity, out var shuttle);
        OnInertiaDampeningModeChanged?.Invoke(shuttle, mode);
    }

    private void NfUpdateState()
    {
        DampenerOff.Pressed = NavRadar.DampeningMode == InertiaDampeningMode.Cruise;
        DampenerOn.Pressed = NavRadar.DampeningMode == InertiaDampeningMode.Dampen;
        AnchorOn.Pressed = NavRadar.DampeningMode == InertiaDampeningMode.Anchor;

        // Disable the Park button (AnchorOn) while in FTL, but keep other dampener buttons enabled
        if (NavRadar.InFtl)
        {
            AnchorOn.Disabled = true;
            // If the AnchorOn button is pressed while it gets disabled, we need to switch to another mode
            if (!AnchorOn.Pressed)
                return;

            DampenerOn.Pressed = true;
            SetDampenerMode(InertiaDampeningMode.Dampen);
        }
        else
            AnchorOn.Disabled = false;
    }

    // Maximum IFF Distance
    private void OnRangeFilterChanged(int value)
    {
        NavRadar.MaximumIFFDistance = value;
    }

    private void OnIffSearchChanged(string text)
    {
        text = text.Trim();

        NavRadar.IFFFilter = text.Length == 0
            ? null // If empty, do not filter
            : (entity, _, _) =>
                _entManager.TryGetComponent<MetaDataComponent>(entity, out var metadata) &&
                metadata.EntityName.Contains(text, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Updates the text on the network port buttons based on the custom port names.
    /// </summary>
    /// <param name="portNames">Dictionary of port IDs to display names</param>
    private void UpdateNetworkPortButtonNames(Dictionary<string, string> portNames)
    {
        // Map of button names to their corresponding port IDs in the component
        var buttonToPortIdMap = new Dictionary<Button, string>
        {
            { DeviceButton1, "SignalShuttleConsole1" },
            { DeviceButton2, "SignalShuttleConsole2" },
            { DeviceButton3, "SignalShuttleConsole3" },
            { DeviceButton4, "SignalShuttleConsole4" },
        };

        // For each button, check if there's a custom name and update accordingly
        foreach (var (button, portId) in buttonToPortIdMap)
        {
            if (portNames.TryGetValue(portId, out var customName))
            {
                // Use the custom name if available
                button.Text = customName;
            }
            else
            {
                // Otherwise use the default localized name
                button.Text = Loc.GetString(_proto.Index<SourcePortPrototype>(portId).Name);
            }
        }
    }
}
