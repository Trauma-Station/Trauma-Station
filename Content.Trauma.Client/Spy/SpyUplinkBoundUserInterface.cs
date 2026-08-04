// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Store;
using Content.Trauma.Shared.Spy;
using Content.Trauma.Shared.Spy.Ui;
using JetBrains.Annotations;
using Robust.Client.Player;

namespace Content.Trauma.Client.Spy;

[UsedImplicitly]
public sealed class SpyUplinkBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IEntityManager _ent = default!;

    [ViewVariables]
    private SpyUplinkMenu? _menu;
    private SpyUplinkSystem? _system;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<SpyUplinkMenu>();
        _system = _ent.System<SpyUplinkSystem>();

        Update();
    }

    public override void Update()
    {
        base.Update();

        if (_player.LocalEntity is not { } player || _menu is not { } menu || _system is not { } sys)
            return;

        if (sys.TryGetSpyRole(player) is not { } role || sys.TryGetSpyRule(role.Comp2) is not { } rule ||
            !_ent.TryGetComponent(rule, out SpyRuleComponent? comp))
            return;

        menu.UpdateRefreshTime(comp.NextRefresh);
        menu.UpdateBounties(comp.CurrentBounties);
        menu.UpdateRewards(role.Comp2.AvailableRewards);

        menu.OnCollect += SendMessage;
    }

    private void SendMessage(string id, ProtoId<ListingPrototype> listing)
    {
        SendPredictedMessage(new SpyRewardSelectedMessage(id, listing));
    }
}
