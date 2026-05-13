// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.UserInterface.Controls;
using Content.Trauma.Client.Heretic.Systems;
using Content.Trauma.Shared.Heretic.Rituals;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface;

namespace Content.Trauma.Client.Heretic.UI;

[UsedImplicitly]
public sealed partial class HereticRitualRuneBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private IPlayerManager _player = default!;

    private SimpleRadialMenu? _menu;

    protected override void Open()
    {
        base.Open();

        if (_player.LocalEntity is not { } player)
            return;

        if (!EntMan.HasComponent<HereticRitualRuneComponent>(Owner))
            return;

        if (!EntMan.System<HereticSystem>().TryGetHereticComponent(player, out var heretic, out _))
            return;

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.Track(Owner);
        var buttonModels = ConvertToButtons(heretic.RitualContainer.ContainedEntities);
        _menu.SetButtons(buttonModels);

        _menu.Open();
    }

    private IEnumerable<RadialMenuActionOption<NetEntity>> ConvertToButtons(
        IReadOnlyList<EntityUid> ents)
    {
        var models = new RadialMenuActionOption<NetEntity>[ents.Count];
        for (var i = 0; i < ents.Count; i++)
        {
            var ent = ents[i];

            models[i] = new RadialMenuActionOption<NetEntity>(HandleRadialMenuClick, EntMan.GetNetEntity(ent))
            {
                IconSpecifier = new RadialMenuEntityIconSpecifier(ent),
                ToolTip = EntMan.GetComponent<MetaDataComponent>(ent).EntityName,
            };
        }

        return models;
    }

    private void HandleRadialMenuClick(NetEntity ent)
    {
        SendPredictedMessage(new HereticRitualMessage(ent));
    }
}
