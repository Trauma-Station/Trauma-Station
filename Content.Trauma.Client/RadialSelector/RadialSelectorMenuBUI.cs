// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Construction;
using Content.Client.UserInterface.Controls;
using Content.Shared.Construction.Prototypes;
using Content.Trauma.Common.RadialSelector;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Trauma.Client.RadialSelector;

public sealed class RadialSelectorMenuBUI : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    private ConstructionSystem _construction;

    public SimpleRadialMenu Menu;

    private Action<string> OnPressed;

    public RadialSelectorMenuBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _construction = EntMan.System<ConstructionSystem>();

        Menu = this.CreateWindow<SimpleRadialMenu>();

        OnPressed = proto =>
        {
            SendPredictedMessage(new RadialSelectorSelectedMessage(proto));
        };
    }

    protected override void Open()
    {
        base.Open();

        Menu.OpenOverMouseScreenPosition();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not RadialSelectorState cast)
            return;

        CreateMenu(cast.Entries);
    }

    private void CreateMenu(List<RadialSelectorEntry> entries)
    {
        Menu.SetButtons(CreateModels(entries));
    }

    private List<RadialMenuOptionBase> CreateModels(List<RadialSelectorEntry> entries)
    {
        var models = new List<RadialMenuOptionBase>();
        foreach (var entry in entries)
        {
            if (entry.Category is {} category)
            {
                var children = CreateModels(category.Entries);
                models.Add(new RadialMenuNestedLayerOption(children)
                {
                    ToolTip = category.Name,
                    IconSpecifier = RadialMenuIconSpecifier.With(category.Icon)
                });
            }
            else if (entry.Prototype is {} proto)
            {
                models.Add(new RadialMenuActionOption<string>(OnPressed, proto)
                {
                    ToolTip = GetName(proto),
                    IconSpecifier = RadialMenuIconSpecifier.With(entry.Icon) ?? GetIcon(proto)
                });
            }
        }

        return models;
    }

    /// <summary>
    /// Get the name for an entity or construction prototype.
    /// </summary>
    private string GetName(string proto)
    {
        if (_proto.TryIndex(proto, out var prototype))
            return prototype.Name;

        if (_proto.Resolve<ConstructionPrototype>(proto, out var construction))
            return construction.Name ?? proto;

        return proto;
    }

    /// <summary>
    /// Get the icon for an entity or construction prototype.
    /// </summary>
    private RadialMenuIconSpecifier? GetIcon(string proto)
    {
        if (!_construction.TryGetRecipePrototype(proto, out var result))
            result = proto;

        return RadialMenuIconSpecifier.With(proto);
    }
}
