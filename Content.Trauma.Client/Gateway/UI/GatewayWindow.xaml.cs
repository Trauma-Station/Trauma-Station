// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Computer;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Shared.Shuttles.BUIStates;
using Content.Trauma.Shared.Gateway;
using Robust.Shared.Timing;

namespace Content.Trauma.Client.Gateway.UI;

[GenerateTypedNameReferences]
public sealed partial class GatewayWindow : FancyWindow,
    IComputerWindow<EmergencyConsoleBoundUserInterfaceState>
{
    [Dependency] private IEntityManager _ent = default!;
    [Dependency] private IGameTiming _timing = default!;

    public event Action<NetEntity>? OpenPortal;
    private List<GatewayDestinationData> _destinations = new();

    public NetEntity NetOwner;
    public EntityUid Owner;

    private NetEntity? _current;
    private TimeSpan _nextReady;

    public GatewayWindow()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);
    }

    public void UpdateState(GatewayBoundUserInterfaceState state)
    {
        _destinations = state.Destinations;
        _current = state.Current;

        Container.RemoveAllChildren();

        if (_destinations.Count == 0)
        {
            Container.AddChild(new BoxContainer()
            {
                HorizontalExpand = true,
                VerticalExpand = true,
                Children =
                {
                    new Label()
                    {
                        Text = Loc.GetString("gateway-window-no-destinations"),
                        HorizontalAlignment = HAlignment.Center
                    }
                }
            });
            return;
        }

        var now = _timing.CurTime;

        foreach (var dest in _destinations)
        {
            var ent = dest.Entity;
            var name = dest.Name;

            var box = new BoxContainer()
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                Margin = new Thickness(5f, 5f),
            };

            // HOW DO I ALIGN THESE GOODER
            var nameLabel = new RichTextLabel()
            {
                VerticalAlignment = VAlignment.Center,
                SetWidth = 156f,
            };

            nameLabel.SetMessage(name);
            box.AddChild(nameLabel);
            // Buffer
            box.AddChild(new Control()
            {
                HorizontalExpand = true,
            });

            bool Pressable() => ent == _current || ent == NetOwner;

            var openButton = new Button()
            {
                Text = Loc.GetString("gateway-window-open-portal"),
                Pressed = Pressable(),
                ToggleMode = true,
                Disabled = now < _nextReady || Pressable(),
                HorizontalAlignment = HAlignment.Right,
                Margin = new Thickness(10f, 0f, 0f, 0f),
                SetHeight = 32f,
            };

            openButton.OnPressed += args =>
            {
                OpenPortal?.Invoke(ent);
            };

            if (Pressable())
            {
                openButton.AddStyleClass(StyleClass.Negative);
            }

            var buttonContainer = new BoxContainer()
            {
                SetSize = new Vector2(128f, 40f),
            };
            buttonContainer.AddChild(openButton);

            box.AddChild(buttonContainer);

            Container.AddChild(new PanelContainer()
            {
                PanelOverride = new StyleBoxFlat(new Color(30, 30, 34)),
                Margin = new Thickness(10f, 5f),
                Children =
                {
                    box
                }
            });
        }
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!_ent.TryGetComponent<GatewayComponent>(Owner, out var comp))
            return;

        _nextReady = comp.NextReady;

        // if its not going to close then show it as empty
        if (_current == null)
        {
            NextReadyBar.Value = 1f;
            NextCloseText.Text = "00:00";
            return;
        }

        var now = _timing.CurTime;
        if (now >= _nextReady)
        {
            NextReadyBar.Value = 1f;
            NextCloseText.Text = "00:00";
        }
        else
        {
            var cooldown = comp.Cooldown;
            var remaining = _nextReady - now;
            NextReadyBar.Value = 1f - (float) (remaining.TotalSeconds / cooldown.TotalSeconds);
            NextCloseText.Text = $"{remaining.Minutes:00}:{remaining.Seconds:00}";
        }
    }
}
