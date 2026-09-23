// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.UserInterface.Controls;
using Content.Trauma.Common.Weapons.AmmoSelector;
using JetBrains.Annotations;

namespace Content.Goobstation.Client.AmmoSelector;

[UsedImplicitly]
public sealed partial class AmmoSelectorMenuBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private IPrototypeManager _proto = default!;

    private SimpleRadialMenu? _menu;

    protected override void Open()
    {
        base.Open();

        if (!EntMan.TryGetComponent(Owner, out AmmoSelectorComponent? selector))
            return;

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.Track(Owner);
        var buttonModels = ConvertToButtons(selector.Prototypes);
        _menu.SetButtons(buttonModels);

        _menu.Open();
    }

    private IEnumerable<RadialMenuActionOption<ProtoId<SelectableAmmoPrototype>>> ConvertToButtons(
        IReadOnlyList<ProtoId<SelectableAmmoPrototype>> protos)
    {
        var models = new RadialMenuActionOption<ProtoId<SelectableAmmoPrototype>>[protos.Count];
        for (var i = 0; i < protos.Count; i++)
        {
            var protoId = protos[i];
            var proto = _proto.Index(protoId);

            models[i] = new RadialMenuActionOption<ProtoId<SelectableAmmoPrototype>>(HandleRadialMenuClick, protoId)
            {
                IconSpecifier = new RadialMenuTextureIconSpecifier(proto.Icon),
                ToolTip = proto.Desc,
            };
        }

        return models;
    }

    private void HandleRadialMenuClick(ProtoId<SelectableAmmoPrototype> protoId)
    {
        SendPredictedMessage(new AmmoSelectedMessage(protoId));
    }
}
