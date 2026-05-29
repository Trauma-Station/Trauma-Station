using Content.Goobstation.Common.Silo;
using Content.Trauma.Common.Salvage;
using Robust.Client.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Lathe.UI;

public sealed partial class LatheMenu
{
    [Dependency] private IPlayerManager _player = default!;
    private CommonMiningPointsSystem _miningPoints = default!;
    private CommonSiloSystem _silo = default!;

    public event Action? OnResetQueueList;
    public event Action? OnClaimMiningPoints;

    public string? AlertLevel;
    private uint? _lastMiningPoints;

    private void InitializeTrauma()
    {
        _miningPoints = _entityManager.System<CommonMiningPointsSystem>();
        _silo = _entityManager.System<CommonSiloSystem>();

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
        if (IsSiloConnected(Entity, out var warning, true))
            return;

        MiningPointsNoConnectionWarning.Visible = true;

        if (warning != null)
            MiningPointsNoConnectionWarning.SetMessage(FormattedMessage.FromMarkupOrThrow(warning));
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
    /// Check if the lathe is connected to a silo, for warning miners.
    /// </summary>
    private bool IsSiloConnected(EntityUid uid, out string? warning, bool checkGrid = false)
    {
        warning = null;
        var silo = _silo.GetSilo(uid);
        if (silo != null
            && checkGrid)
        {
            if (_entityManager.TryGetComponent<TransformComponent>(uid, out var uidTransform)
                && _entityManager.TryGetComponent<TransformComponent>(silo.Value, out var siloTransform))
            {
                if (uidTransform.MapID != siloTransform.MapID)
                {
                    warning = Loc.GetString("lathe-menu-mining-points-silo-not-on-same-grid");
                    return false;
                }

                return true;
            }

            warning = Loc.GetString("lathe-menu-mining-points-silo-not-on-same-grid");
            return false;
        }

        if (silo == null)
            warning = Loc.GetString("lathe-menu-mining-points-no-connection-warning");

        return silo != null;
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
