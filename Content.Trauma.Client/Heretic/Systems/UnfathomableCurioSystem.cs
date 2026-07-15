// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Client.Overlays;
using Content.Trauma.Shared.Heretic.Components.Side;
using Content.Trauma.Shared.Heretic.Systems.Side;

namespace Content.Trauma.Client.Heretic.Systems;

public sealed partial class UnfathomableCurioSystem : SharedUnfathomableCurioSystem
{
    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private ShaderCacheSystem _cache = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay.AddOverlay(new CurioShieldOverlay());
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlay.RemoveOverlay<CurioShieldOverlay>();
    }

    protected override void ResetShield(Entity<UnfathomableCurioShieldComponent> ent, bool playSound, EntityUid? origin, bool resetDeactivateTime = true)
    {
        base.ResetShield(ent, playSound, origin, resetDeactivateTime);

        _cache.RemoveShader(ent, nameof(UnfathomableCurioShieldComponent));
    }
}
