using System.Linq;
using Content.Trauma.Shared.Spy.Ui;
using JetBrains.Annotations;

namespace Content.Trauma.Client.Spy;

[UsedImplicitly]
public sealed class SpyUplinkBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
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

        if (state is not SpyUpdateState spyState || _menu is not { } menu)
            return;

        menu.UpdateRefreshTime(spyState.NextRefresh);
        menu.UpdateBounties(spyState.Bounties.ToList());
    }
}
