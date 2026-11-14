// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Client.Stylesheets.Colorspace;
using Content.Client.UserInterface.Controls;
using Content.Trauma.Shared.Clothing;
using Content.Trauma.Shared.Clothing.MultiToggleable;

namespace Content.Trauma.Client.Clothing;

public sealed partial class MultiToggleableClothingBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private SimpleRadialMenu? _menu;

    protected override void Open()
    {
        base.Open();

        if (!EntMan.TryGetComponent(Owner, out MultiToggleableClothingComponent? toggleable))
            return;

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.CloseOnPressed = false;
        _menu.Track(Owner);
        var buttonModels = ConvertToButtons(toggleable);
        _menu.SetButtons(buttonModels);

        _menu.Open();
    }

    private IEnumerable<RadialMenuActionOption<EntityUid>> ConvertToButtons(
        MultiToggleableClothingComponent comp)
    {
        var clothing = comp.ClothingUids.Keys.ToList();
        var models = new RadialMenuActionOption<EntityUid>[clothing.Count];
        for (var i = 0; i < clothing.Count; i++)
        {
            var uid = clothing[i];
            var color = comp.Container.Contains(uid) ? Color.Red : Color.Lime;
            models[i] = new RadialMenuActionOption<EntityUid>(HandleRadialMenuClick, uid)
            {
                IconSpecifier = new RadialMenuEntityIconSpecifier(uid),
                ToolTip = EntMan.GetComponent<MetaDataComponent>(uid).EntityName,
                BorderColor = color,
                HoverBorderColor = color.WithLightness(0.75f),
            };
        }

        return models;
    }

    public override void Update()
    {
        base.Update();

        if (_menu is not { } menu || !menu.IsOpen)
            return;

        if (!EntMan.TryGetComponent(Owner, out MultiToggleableClothingComponent? toggleable))
            return;

        var buttonModels = ConvertToButtons(toggleable);
        menu.SetButtons(buttonModels);
    }

    private void HandleRadialMenuClick(EntityUid uid)
    {
        SendPredictedMessage(new MultiToggleableClothingUiMessage(EntMan.GetNetEntity(uid)));
    }
}
