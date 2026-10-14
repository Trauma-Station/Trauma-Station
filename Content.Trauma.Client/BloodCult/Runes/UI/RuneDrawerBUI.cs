// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Client.UserInterface.Controls;
using Content.Trauma.Shared.BloodCult.Runes;

namespace Content.Trauma.Client.BloodCult.Runes.UI;

public sealed partial class RuneDrawerBUI(EntityUid owner, Enum key) : BoundUserInterface(owner, key)
{
    [Dependency] private IPrototypeManager _proto = default!;

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
        var runes = _proto.EnumeratePrototypes<BloodRunePrototype>()
            .OrderBy(r => r.ID)
            .ToList();

        var options = new List<RadialMenuOptionBase>(runes.Count);
        foreach (var rune in runes)
        {
            if (!_proto.Resolve(rune.Prototype, out var proto))
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
        SendPredictedMessage(new RuneDrawerSelectedMessage(id));
        Close();
    }
}
