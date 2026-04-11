// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.UserInterface.Controls;
using Content.Shared.Polymorph;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Rituals;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface;

namespace Content.Trauma.Client.Heretic.UI;

[UsedImplicitly]
public sealed class HereticShapeshiftBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private SimpleRadialMenu? _menu;

    protected override void Open()
    {
        base.Open();

        if (_player.LocalEntity is not { } player)
            return;

        if (!EntMan.TryGetComponent(Owner, out ShapeshiftActionComponent? shapeshift))
            return;

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.Track(player);
        var buttonModels = ConvertToButtons(shapeshift.Polymorphs);
        _menu.SetButtons(buttonModels);

        _menu.Open();
    }

    private IEnumerable<RadialMenuActionOption<ProtoId<PolymorphPrototype>>> ConvertToButtons(
        IReadOnlyList<ProtoId<PolymorphPrototype>> protos)
    {
        var models = new RadialMenuActionOption<ProtoId<PolymorphPrototype>>[protos.Count];
        for (var i = 0; i < protos.Count; i++)
        {
            var protoId = protos[i];
            var proto = _proto.Index(protoId);

            if (proto.Configuration.Entity is not { } entity)
                throw new ArgumentException($"Expected {proto.ID} configuration Entity to not be null");

            models[i] = new RadialMenuActionOption<ProtoId<PolymorphPrototype>>(HandleRadialMenuClick, protoId)
            {
                IconSpecifier = new RadialMenuEntityPrototypeIconSpecifier(entity),
                ToolTip = _proto.Index(entity).Name,
            };
        }

        return models;
    }

    private void HandleRadialMenuClick(ProtoId<PolymorphPrototype> protoId)
    {
        SendMessage(new HereticShapeshiftMessage(protoId));
    }
}
