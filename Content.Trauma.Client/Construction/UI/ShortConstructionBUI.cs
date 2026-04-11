// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Client.Construction;
using Content.Client.UserInterface.Controls;
using Content.Shared.Construction.Prototypes;
using Content.Trauma.Shared.Construction;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Placement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Enums;

namespace Content.Trauma.Client.Construction.UI;

public sealed class ShortConstructionBUI : BoundUserInterface
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IPlacementManager _placement = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    private readonly ConstructionSystem _construction;
    private readonly SpriteSystem _sprite;

    private RadialMenu? _menu;

    public ShortConstructionBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _construction = EntMan.System<ConstructionSystem>();
        _sprite = EntMan.System<SpriteSystem>();
    }

    protected override void Open()
    {
        base.Open();

        _menu = CreateMenu();
        _menu.OpenCenteredAt(_input.MouseScreenPosition.Position / _clyde.ScreenSize);
    }

    private RadialMenu CreateMenu()
    {
        var menu = this.CreateWindow<RadialMenu>();
        menu.HorizontalExpand = true;
        menu.VerticalExpand = true;
        menu.BackButtonStyleClass = "RadialMenuBackButton";
        menu.CloseButtonStyleClass = "RadialMenuCloseButton";
        menu.OnClose += Close;

        if (!EntMan.TryGetComponent<ShortConstructionComponent>(Owner, out var comp))
            return menu;

        var options = new RadialContainer();
        foreach (var protoId in comp.Prototypes)
        {
            if (!_proto.Resolve(protoId, out var proto) ||
                !_construction.TryGetRecipePrototype(protoId, out var targetId) ||
                !_proto.Resolve(targetId, out var target))
                continue;

            var button = new RadialMenuButton
            {
                ToolTip = proto.SetName is {} loc ? Loc.GetString(loc) : target.Name,
                StyleClasses = { "RadialMenuButton" },
                SetSize = new Vector2(48f, 48f)
            };

            var texture = new TextureRect
            {
                VerticalAlignment = Control.VAlignment.Center,
                HorizontalAlignment = Control.HAlignment.Center,
                Texture = _sprite.Frame0(target),
                TextureScale = new Vector2(2f, 2f)
            };

            button.AddChild(texture);

            button.OnPressed += _ =>
            {
                ConstructItem(proto);
            };

            options.AddChild(button);
        }

        menu.AddChild(options);
        return menu;
    }

    /// <summary>
    /// Makes an item or starts placing a construction ghost based on the type of construction recipe.
    /// You still have to actually place the ghost yourself for structures.
    /// </summary>
    private void ConstructItem(ConstructionPrototype prototype)
    {
        if (prototype.Type == ConstructionType.Item)
        {
            _construction.TryStartItemConstruction(prototype.ID);
            return;
        }

        _placement.BeginPlacing(new PlacementInformation
        {
            IsTile = false,
            PlacementOption = prototype.PlacementMode
        }, new ConstructionPlacementHijack(_construction, prototype));

        _menu?.Close();
    }
}
