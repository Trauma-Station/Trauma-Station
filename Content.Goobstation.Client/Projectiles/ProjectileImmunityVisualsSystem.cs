// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Projectiles;
using Robust.Client.Graphics;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Goobstation.Client.Projectiles;

public sealed class DodgeEffectVisualsSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay.AddOverlay(new DodgeEffectOverlay());

        SubscribeLocalEvent<DodgeEffectComponent, ComponentStartup>(OnStartup);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<DodgeEffectOverlay>();
    }

    private void OnStartup(Entity<DodgeEffectComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.Time = _timing.RealTime;
        ent.Comp.Seed = _random.NextFloat() * 1000f;
    }
}
