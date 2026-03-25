using Content.DV.Common.Salvage;
using Content.Goobstation.Common.Silo;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.Lathe.UI;

public sealed partial class LatheMenu
{
    [Dependency] private readonly IPlayerManager _player = default!; // DeltaV

    private readonly CommonMiningPointsSystem _miningPoints; // DeltaV
    private readonly CommonSiloSystem _silo; // Goobstation
    public event Action<BaseButton.ButtonEventArgs>? OnResetQueueListButtonPressed; // Goobstation
    public event Action? OnClaimMiningPoints; // DeltaV

    public string? AlertLevel; // Trauma
    private uint? _lastMiningPoints; // DeltaV: used to avoid Loc.GetString every frame


    /// <summary>
    /// DeltaV: Updates the UI elements for mining points.
    /// </summary>
    private void UpdateMiningPoints(uint points)
    {
        MiningPointsClaimButton.Disabled = points == 0 ||
            _player.LocalSession?.AttachedEntity is not { } player ||
            !_miningPoints.CanClaimPoints(player); // Goobstation - borg mining Points
        if (points == _lastMiningPoints)
            return;

        _lastMiningPoints = points;
        MiningPointsLabel.Text = Loc.GetString("lathe-menu-mining-points", ("points", points));
    }

    /// <summary>
    /// DeltaV: Update mining points UI whenever it changes.
    /// </summary>
    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_entityManager.TryGetComponent<MiningPointsComponent>(Entity, out var points))
            UpdateMiningPoints(points.Points);
    }

    /// <summary>
    /// Goobstation: Check if the lathe is connected to a silo.
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

}
