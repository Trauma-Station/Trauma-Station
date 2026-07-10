// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Timing;

namespace Content.Goobstation.Client.Physics;

public sealed partial class ComplexJointVisualsSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new ComplexJointVisualsOverlay(EntityManager, ProtoMan, _timing));
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<ComplexJointVisualsOverlay>();
    }
}
