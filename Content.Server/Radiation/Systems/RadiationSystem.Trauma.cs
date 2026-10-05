using Content.Trauma.Common.Radiation;
using Robust.Shared.Player;

namespace Content.Server.Radiation.Systems;

public sealed partial class RadiationSystem
{
    [Dependency] private ISharedPlayerManager _player = default!;

    /// <summary>
    /// When an entity gets irradiated, if that entity has a player attached, send a network event to that player to update their radiation VFX
    /// </summary>
    public void UpdateRadiationVisuals(EntityUid uid, float rads)
    {
        if (!_player.TryGetSessionByEntity(uid, out var session)) return;

        var msg = new IrradiatedNetworkEvent(rads);
        RaiseNetworkEvent(msg, session);
    }
}
