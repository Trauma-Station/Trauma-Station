// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.UserInterface.Controls;
using Content.Trauma.Client.Heretic.Systems;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Events;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Shared.Timing;

namespace Content.Trauma.Client.Heretic.UI;

[UsedImplicitly]
public sealed partial class LivingHeartMenuBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IEntityManager _ent = default!;
    [Dependency] private IGameTiming _timing = default!;

    private SimpleRadialMenu? _menu;

    private static readonly EntProtoId Fallback = "CodexCicatrix";

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
        var spriteSys = _ent.System<SpriteSystem>();
        for (var i = 0; i < datas.Count; i++)
        {
            var data = datas[i];

            models[i] = new RadialMenuActionOption<NetEntity>(HandleRadialMenuClick, data.Entity)
            {
                IconSpecifier = _ent.TryGetEntity(data.Entity, out var e)
                    ? new RadialMenuEntityIconSpecifier(e.Value)
                    : new RadialMenuEntityPrototypeIconSpecifier(Fallback),
                ToolTip = data.Name,
            };
        }

        return models;
    }

    private void HandleRadialMenuClick(NetEntity ent)
    {
        var comp = _ent.EnsureComponent<HereticSacrificeTargetComponent>(_ent.GetEntity(ent));
        comp.RemovalTimer = _timing.CurTime + comp.RemovalTime;
        SendMessage(new EventHereticLivingHeartActivate(ent));
    }
}
