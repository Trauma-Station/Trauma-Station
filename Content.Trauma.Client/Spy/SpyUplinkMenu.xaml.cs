using System.Linq;
using Content.Client.UserInterface.Controls;
using Content.Trauma.Shared.Spy;
using Robust.Shared.Timing;

namespace Content.Trauma.Client.Spy;

[GenerateTypedNameReferences]
public sealed partial class SpyUplinkMenu : FancyWindow
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    private List<SpyBounty> _cachedBounties = new();

    private TimeSpan _nextRefresh;

    public SpyUplinkMenu()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);
    }

    public void UpdateRefreshTime(TimeSpan nextRefresh)
    {
        _nextRefresh = nextRefresh;
        UpdateRefreshTime();
    }

    public void UpdateTabs()
    {
        SpyTabs.SetTabTitle(0, Loc.GetString("spy-uplink-bounties"));
        SpyTabs.SetTabTitle(1, Loc.GetString("spy-uplink-rewards"));
    }

    public void UpdateBounties(List<SpyBounty> bounties)
    {
        _cachedBounties = bounties;

        UpdateBounties();
    }

    public void UpdateBounties()
    {
        var sorted = _cachedBounties.OrderBy(l => _proto.Index(l.BountyProto).Difficulty).ThenBy(l => l.Name);

        ClearBounties();
        foreach (var item in sorted)
        {
            AddListingGui(item);
        }
    }

    private void AddListingGui(SpyBounty bounty)
    {
        var newBounty = new SpyBountyControl(bounty);

        BountiesContainer.AddChild(newBounty);
    }

    private void ClearBounties()
    {
        BountiesContainer.Children.Clear();
    }

    public void UpdateRefreshTime()
    {
        var difference = _nextRefresh - _timing.CurTime;
        RefreshTimeLabel.Text = Loc.GetString("spy-uplink-refresh-time", ("time", $"{difference:mm\\:ss}"));
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        UpdateRefreshTime();
    }
}
