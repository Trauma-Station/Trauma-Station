// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Construction;
using Content.Client.UserInterface.Controls;
using Content.Shared.Construction.Prototypes;
using Content.Trauma.Shared.Construction;
using Robust.Client.Placement;
using Robust.Shared.Enums;

namespace Content.Trauma.Client.Construction.UI;

public sealed partial class ShortConstructionBUI : BoundUserInterface
{
    [Dependency] private IPlacementManager _placement = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    private readonly ConstructionSystem _construction;

    private SimpleRadialMenu? _menu;

    public ShortConstructionBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _construction = EntMan.System<ConstructionSystem>();
    }

    protected override void Open()
    {
        base.Open();

        _menu = CreateMenu();
        _menu.OpenOverMouseScreenPosition();
    }

    private SimpleRadialMenu CreateMenu()
    {
        var menu = this.CreateWindow<SimpleRadialMenu>();
        menu.CloseOnPressed = false; // custom closing logic

        if (!EntMan.TryGetComponent<ShortConstructionComponent>(Owner, out var comp))
            return menu;

        var options = new List<RadialMenuOptionBase>();
        foreach (var protoId in comp.Prototypes)
        {
            if (!_proto.Resolve(protoId, out var proto) ||
                !_construction.TryGetRecipePrototype(protoId, out var targetId) ||
                !_proto.Resolve(targetId, out var target))
                continue;

            var tooltip = proto.SetName is {} loc ? Loc.GetString(loc) : target.Name;
            options.Add(new RadialMenuActionOption<ConstructionPrototype>(ConstructItem, proto)
            {
                ToolTip = tooltip,
                IconSpecifier = RadialMenuIconSpecifier.With(targetId)
            });
        }

        menu.SetButtons(options);
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
        }, new ConstructionPlacementHijack(prototype));

        _menu?.Close();
    }
}
