// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.BloodCult.Spells;
using Content.Trauma.Shared.BloodCult.UI;

namespace Content.Trauma.Client.BloodCult.UI;

public sealed partial class CultSpellsUiKeyBoundUserInterface : BoundUserInterface
{
    // this is also not done yet
    private SimpleRadialMenu? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.SetButtons(GetButtons());
        _menu.OpenOverMouseScreenPosition();
    }

    private List<RadialMenuOptionBase> GetButtons()
    {
        var spells = _proto.EnumeratePrototypes<BloodRunePrototype>()
            .OrderBy(r => r.ID)
            .ToList();

        var options = new List<RadialMenuOptionBase>(spells.Count);
        foreach (var spell in spells)
        {
            if (!_proto.Resolve(spell.Prototype, out var proto))
                continue;

            options.Add(new RadialMenuActionOption<ProtoId<BloodRunePrototype>>(OnSelected, proto.ID)
            {
                ToolTip = proto.Name,
                IconSpecifier = RadialMenuIconSpecifier.With(rune.Prototype)
            });
        }

        return options;
    }

    private void OnSelected(ProtoId<BloodRunePrototype> id)
    {
        SendPredictedMessage(new CultSpellsSelectMessage(id));
        Close();
    }
}
