// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Trauma.Shared.ClockworkCult.Power.Components;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Client.ClockworkCult;

public sealed class ClockworkTransferOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly SharedTransformSystem _transformSystem;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    /// <summary>
    /// The thickness of the beam used to connect clockwork structures with each other.
    /// </summary>
    public float BeamThickness = 0.1f;

    public ClockworkTransferOverlay()
    {
        IoCManager.InjectDependencies(this);

        _transformSystem = _entMan.System<SharedTransformSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var query = _entMan.EntityQueryEnumerator<ClockworkTransferrerComponent, ClockworkConnectionHolderComponent>();
        while (query.MoveNext(out var uid, out var transfer, out var connectionHolder))
        {
            var sourceTransform = _entMan.GetComponent<TransformComponent>(uid);
            if (sourceTransform.MapID == MapId.Nullspace)
                continue;

            var sourcePos = _transformSystem.GetWorldPosition(sourceTransform);

            var connectionColor = Color.Beige; // default color if it fails to find prototype
            if (_prototype.TryIndex(connectionHolder.TransferConnection, out var connectionType))
                connectionColor = connectionType.ConnectionColor;

            foreach (var connection in transfer.Connections)
            {
                if (_entMan.Deleted(connection))
                    continue;

                var linkTransform = _entMan.GetComponent<TransformComponent>(connection);
                if (linkTransform.MapID == MapId.Nullspace)
                    continue;

                // Note: GPT math
                var targetPos = _transformSystem.GetWorldPosition(linkTransform);
                var length = (targetPos - sourcePos).Length();
                var angle = (targetPos - sourcePos).ToAngle();
                var midPoint = (sourcePos + targetPos) / 2f;
                var box = new Box2(-length / 2f, -BeamThickness / 2f, length / 2f, BeamThickness / 2f);
                var transform = Matrix3Helpers.CreateTransform(midPoint, angle);

                args.WorldHandle.SetTransform(transform);
                args.WorldHandle.DrawRect(box, connectionColor);
            }
        }

        args.WorldHandle.SetTransform(Matrix3x2.Identity);
    }
}
