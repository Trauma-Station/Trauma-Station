// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.UserInterface.Controls;
using Content.Trauma.Shared.BloodCult.Spells;

namespace Content.Trauma.Client.BloodCult.Spells.UI;

public sealed partial class CultSpellsBUI(EntityUid owner, Enum key) : BoundUserInterface(owner, key)
{
    [Dependency] private IPrototypeManager _proto = default!;

    private SimpleRadialMenu? _menu;

    protected override void Open()
    {
        base.Open();

        if (!EntMan.TryGetComponent<BloodCultSpellsComponent>(Owner, out var comp))
            return;

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.SetButtons(GetButtons(comp));
        _menu.OpenOverMouseScreenPosition();
    }

    private List<RadialMenuOptionBase> GetButtons(BloodCultSpellsComponent comp)
    {
        var count = comp.AvailableActions.Count;
        var options = new List<RadialMenuOptionBase>(count);
        for (var i = 0; i < count; i++)
        {
            var id = comp.AvailableActions[i];
            var proto = _proto.Index(id);
            options.Add(new RadialMenuActionOption<int>(OnSelected, i)
            {
                ToolTip = $"{proto.Name}\n{proto.Description}",
                IconSpecifier = RadialMenuIconSpecifier.With(id)
            });
        }

        return options;
    }

    private void OnSelected(int i)
    {
        SendPredictedMessage(new CultSpellSelectedMessage(i));
        Close();
    }
}
