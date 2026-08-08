// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Client.UserInterface.Controls;
using Content.Shared.Store;
using Content.Trauma.Shared.Spy;
using Robust.Shared.Timing;

namespace Content.Trauma.Client.Spy;

[GenerateTypedNameReferences]
public sealed partial class SpyUplinkMenu : FancyWindow
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    public event Action<string, ProtoId<ListingPrototype>>? OnCollect;

    private HashSet<SpyBounty> _cachedBounties = new();
    private List<string> _cachedRewards = new();

    private TimeSpan _nextRefresh;

    public SpyUplinkMenu()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        UpdateTabs();
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

    public void UpdateRewards(List<string> rewards)
    {
        _cachedRewards = rewards;

        UpdateRewards();
    }

    public void UpdateRewards()
    {
        ClearRewards();

        if (NoRewardsRefreshAvailable())
            return;

        _cachedRewards.Sort();

        foreach (var item in _cachedRewards)
        {
            AddRewardGui(item);
        }
    }

    public bool NoRewardsRefreshAvailable()
    {
        var noRewards = _cachedRewards.Count == 0;

        NoRewardsLabel.Visible = noRewards;
        RewardsScroll.Visible = !noRewards;

        return noRewards;
    }

    private void ClearRewards()
    {
        RewardsContainer.Children.Clear();
    }

    private void AddRewardGui(string reward)
    {
        var newReward = new SpyRewardControl(reward);
        newReward.OnCollect += (control, id, proto) =>
        {
            _cachedRewards.Remove(id);
            RewardsContainer.RemoveChild(control);
            NoRewardsRefreshAvailable();

            OnCollect?.Invoke(id, proto);
        };

        RewardsContainer.AddChild(newReward);
    }

    public void UpdateBounties(HashSet<SpyBounty> bounties)
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

    private void ClearBounties()
    {
        BountiesContainer.Children.Clear();
    }

    private void AddListingGui(SpyBounty bounty)
    {
        var newBounty = new SpyBountyControl(bounty);

        BountiesContainer.AddChild(newBounty);
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
