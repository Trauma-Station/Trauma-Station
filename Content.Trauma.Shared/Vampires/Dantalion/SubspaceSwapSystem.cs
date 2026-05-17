// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Trauma.Shared.Teleportation;

namespace Content.Trauma.Shared.Vampires.Dantalion;

/// <summary>
/// Action that swaps your entity's positions with another one's.
/// </summary>
public sealed partial class SubspaceSwapSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SubspaceSwapActionEvent>(OnSwap);
    }

    private void OnSwap(SubspaceSwapActionEvent args)
    {
        var performer = args.Performer;
        var target = args.Target;

        _transform.SwapPositions(performer, target);

        args.Handled = true;
    }
}

public sealed partial class SubspaceSwapActionEvent : EntityTargetActionEvent;
