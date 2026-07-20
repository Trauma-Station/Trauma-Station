using System.Linq;
using Content.Shared.Store;
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

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<SpyUplinkMenu>();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not SpyUpdateState spyState || _player.LocalEntity is not { } player || _menu is not { } menu)
            return;

        menu.UpdateRefreshTime(spyState.NextRefresh);
        menu.UpdateBounties(spyState.Bounties.ToList());
        menu.UpdateRewards(spyState.Rewards[_ent.GetNetEntity(player)]);

        menu.OnCollect += SendMessage;
    }

    private void SendMessage(string id, ProtoId<ListingPrototype> listing)
    {
        SendMessage(new SpyRewardSelectedMessage(id, listing));
    }
}
