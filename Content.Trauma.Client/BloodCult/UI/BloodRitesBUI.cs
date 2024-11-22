// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Popups;
using Content.Client.UserInterface.Controls;
using Content.Shared.FixedPoint;
using Content.Trauma.Shared.BloodCult.BloodRites;
using Content.Trauma.Shared.BloodCult.UI;

namespace Content.Trauma.Client.BloodCult.UI;

public sealed partial class BloodRitesBUI : BoundUserInterface
{
    [Dependency] private IPrototypeManager _proto = default!;
    private PopupSystem _popup;

    private static readonly Vector2 _itemSize = Vector2.One * 64;

    private SimpleRadialMenu? _menu;

    public BloodRitesBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _popup = EntMan.System<PopupSystem>();

        _menu = this.CreateWindow<SimpleRadialMenu>();

        if (!EntMan.TryGetComponent<BloodRitesAuraComponent>(owner, out var comp))
            return;

        _menu.SetButtons(GetButtons(comp.Crafts));
    }

    protected override void Open()
    {
        base.Open();

        _menu.OpenOverMouseScreenPosition();
    }

    private List<RadialMenuOptionBase> GetButtons(Dictionary<EntProtoId, FixedPoint2> crafts)
    {
        var options = new List<RadialMenuOptionBase>();
        foreach (var (id, cost) in crafts)
        {
            if (!_proto.Resolve(id, out var proto))
                continue;

            var name = $"{cost}: {proto.Name}";
            options.Add(new RadialMenuActionOption<EntProtoId>(TryCraft, id)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(id),
                ToolTip = name
            });
        }

        return options;
    }

    private void TryCraft(EntProtoId id)
    {
        if (!EntMan.TryGetComponent<BloodRitesAuraComponent>(Owner, out var comp))
            return;

        var cost = comp.Crafts[id];
        if (cost > comp.StoredBlood)
        {
            _popup.PopupEntity(Loc.GetString("blood-rites-not-enough-blood"), Owner);
            return;
        }

        var msg = new BloodRitesMessage(id);
        SendPredictedMessage(msg);
    }
}
