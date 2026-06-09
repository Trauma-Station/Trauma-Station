// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Movement.Systems;
using Content.Trauma.Common.MouseWheel;

namespace Content.Trauma.Shared.Camera;

public sealed partial class CameraRotateSystem : EntitySystem
{
    [Dependency] private SharedMoverController _controller = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeAllEvent<RotateCameraEvent>(OnRotate);
    }

    private void OnRotate(RotateCameraEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is { } ent)
            _controller.RotateCamera(ent, msg.Rotation);
    }
}
