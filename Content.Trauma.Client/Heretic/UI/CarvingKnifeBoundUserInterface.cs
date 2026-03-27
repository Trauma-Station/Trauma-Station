// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.UserInterface.Controls;
using Content.Trauma.Shared.Heretic.Components.Side.Carvings;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Client.Heretic.UI;

[UsedImplicitly]
public sealed class CarvingKnifeBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private SimpleRadialMenu? _menu;

    protected override void Open()
    {
        base.Open();

        if (!EntMan.TryGetComponent(Owner, out CarvingKnifeComponent? knife))
            return;

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.Track(Owner);
        var buttonModels = ConvertToButtons(knife.Carvings);
        _menu.SetButtons(buttonModels);

        _menu.Open();
    }

    private IEnumerable<RadialMenuActionOption<EntProtoId>> ConvertToButtons(
        IReadOnlyList<EntProtoId> protos)
    {
        var models = new RadialMenuActionOption<EntProtoId>[protos.Count];
        for (var i = 0; i < protos.Count; i++)
        {
            var protoId = protos[i];
            var proto = _proto.Index(protoId);

            models[i] = new RadialMenuActionOption<EntProtoId>(HandleRadialMenuClick, protoId)
            {
                IconSpecifier = proto.TryGetComponent(out IconComponent? icon)
                    ? new RadialMenuTextureIconSpecifier(icon.Icon)
                    : new RadialMenuEntityPrototypeIconSpecifier(proto),
                ToolTip = Loc.GetString("carving-knife-ui-tooltip", ("name", proto.Name), ("desc", proto.Description)),
            };
        }

        return models;
    }


    private void HandleRadialMenuClick(EntProtoId protoId)
    {
        SendMessage(new RuneCarvingSelectedMessage(protoId));
    }
}
