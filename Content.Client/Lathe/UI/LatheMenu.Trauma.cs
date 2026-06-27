using Content.Trauma.Common.Salvage;
using Robust.Client.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Lathe.UI;

public sealed partial class LatheMenu
{
    [Dependency] private IPlayerManager _player = default!;
    private CommonMiningPointsSystem _miningPoints = default!;

    public event Action? OnResetQueueList;
    public event Action? OnClaimMiningPoints;

    public string? AlertLevel;
    private uint? _lastMiningPoints;

    private void InitializeTrauma()
    {
        _miningPoints = _entityManager.System<CommonMiningPointsSystem>();

        ResetQueueList.OnPressed += _ => OnResetQueueList?.Invoke();
    }

    private void UpdateMiningPoints()
    {
        MiningPointsContainer.Visible = _entityManager.TryGetComponent<MiningPointsComponent>(Entity, out var points);
        MiningPointsClaimButton.OnPressed += _ => OnClaimMiningPoints?.Invoke();

        MaterialsList.SetOwner(Entity);

        if (points is null)
            return;

        UpdateMiningPoints(points.Points);
    }

    /// <summary>
    /// Updates the UI elements for mining points.
    /// </summary>
    private void UpdateMiningPoints(uint points)
    {
        MiningPointsClaimButton.Disabled = points == 0 ||
            _player.LocalSession?.AttachedEntity is not { } player ||
            !_miningPoints.CanClaimPoints(player);
        if (points == _lastMiningPoints)
            return;

        _lastMiningPoints = points;
        MiningPointsLabel.Text = Loc.GetString("lathe-menu-mining-points", ("points", points));
    }

    /// <summary>
    /// Update mining points UI whenever it changes.
    /// </summary>
    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_entityManager.TryGetComponent<MiningPointsComponent>(Entity, out var points))
            UpdateMiningPoints(points.Points);
    }
}
