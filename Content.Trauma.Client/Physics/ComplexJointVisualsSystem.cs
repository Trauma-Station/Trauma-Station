// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Physics.ComplexJoint;

namespace Content.Trauma.Client.Physics;

public sealed partial class ComplexJointVisualsSystem : SharedComplexJointVisualsSystem
{
    [Dependency] private IOverlayManager _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new ComplexJointVisualsOverlay(EntityManager, ProtoMan, Timing));
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<ComplexJointVisualsOverlay>();
    }
}
