// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Lobby.UI.ProfileEditorControls;
using Content.Client.UserInterface.Controls;
using Content.Trauma.Client.Heretic.Systems;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Events;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Trauma.Client.Heretic.UI;

[UsedImplicitly]
public sealed class LivingHeartMenuBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private SimpleRadialMenu? _menu;

    protected override void Open()
    {
        base.Open();

        if (_player.LocalEntity is not { } player)
            return;

        if (!EntMan.System<HereticSystem>().TryGetHereticComponent(player, out var heretic, out _))
            return;

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.Track(player);
        var buttonModels = ConvertToButtons(heretic.SacrificeTargets);
        _menu.SetButtons(buttonModels);

        _menu.Open();
    }

    private IEnumerable<RadialMenuActionOption<NetEntity>> ConvertToButtons(IReadOnlyList<SacrificeTargetData> datas)
    {
        var models = new RadialMenuActionOption<NetEntity>[datas.Count];
        for (var i = 0; i < datas.Count; i++)
        {
            var data = datas[i];

            var view = new ProfilePreviewSpriteView();
            view.LoadPreview(data.Profile, _proto.Index(data.Job));

            models[i] = new RadialMenuActionOption<NetEntity>(HandleRadialMenuClick, data.Entity)
            {
                IconSpecifier = new RadialMenuEntityIconSpecifier(view.Entity.GetValueOrDefault()),
                ToolTip = data.Profile.Name,
            };
        }

        return models;
    }

    private void HandleRadialMenuClick(NetEntity ent)
    {
        SendMessage(new EventHereticLivingHeartActivate(ent));
    }
}
