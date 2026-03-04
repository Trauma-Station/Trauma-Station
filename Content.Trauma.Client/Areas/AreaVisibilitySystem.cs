// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.UserInterface.Systems.Sandbox.Windows;
using Content.Trauma.Common.Areas;
using Content.Trauma.Shared.Areas;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Map;

namespace Content.Trauma.Client.Areas;

/// <summary>
/// Controls visibility of areas via the <c>showareas</c> and mapping commands.
/// </summary>
public sealed class AreaVisibilitySystem : CommonAreaVisibilitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private bool _visible;

    public const string ButtonName = "ShowAreasButton";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AreaComponent, ComponentStartup>(OnStartup);

        SandboxWindow.OnOpened += OnOpened;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        SandboxWindow.OnOpened -= OnOpened;
    }

    public override void SetVisible(bool visible)
    {
        if (_visible == visible)
            return;

        _visible = visible;
        UpdateAreas();
    }

    public void ToggleVisibility()
    {
        SetVisible(!_visible);
    }

    private void OnStartup(Entity<AreaComponent> ent, ref ComponentStartup args)
    {
        UpdateVisibility(ent);
    }

    private void UpdateVisibility(EntityUid uid)
    {
        if (Transform(uid).MapID == MapId.Nullspace)
            return;

        _sprite.SetVisible(uid, _visible);
    }

    private void UpdateAreas()
    {
        var query = AllEntityQuery<AreaComponent>(); // include paused for mapping
        while (query.MoveNext(out var uid, out _))
        {
            UpdateVisibility(uid);
        }
    }

    #region UI shit

    private void OnOpened(SandboxWindow window)
    {
        if (EnsureButton(window) is not {} button)
        {
            Log.Error("Failed to add a toggle areas button to the sandbox window!");
            return;
        }

        button.Pressed = _visible;
    }

    private Button? EnsureButton(SandboxWindow window)
    {
        // cant use NameScope because you arent allowed to register after xaml loads
        // have to do this dogshit instead :))))
        foreach (var child in window.Buttons.Children)
        {
            if (child.Name == ButtonName)
                return (Button) child;
        }

        // want to have the areas button below the markers button, so markers is above areas
        var above = window.ShowMarkersButton;
        var index = above.GetPositionInParent() + 1;

        var button = new Button()
        {
            Name = ButtonName,
            ToggleMode = true,
            Text = Loc.GetString("sandbox-window-show-areas-button")
        };
        button.OnPressed += _ => ToggleVisibility();
        // now position it below markers button
        window.Buttons.AddChild(button);
        button.SetPositionInParent(index);
        return button;
    }

    #endregion
}
