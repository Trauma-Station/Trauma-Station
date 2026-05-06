using Content.Shared.Actions;
using Content.Trauma.Shared.Teleportation;

namespace Content.Trauma.Shared.Vampires.Dantalion;

/// <summary>
/// Action that swaps your entity's positions with another one's.
/// </summary>
public sealed class SubspaceSwapSystem : EntitySystem
{
    [Dependency] private readonly TeleportSystem _teleport = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SubspaceSwapActionEvent>(OnSwap);
    }

    private void OnSwap(SubspaceSwapActionEvent args)
    {
        var performer = args.Performer;
        var target = args.Target;

        var xformPerformer = Transform(performer);
        var xformTarget = Transform(target);

        _teleport.Teleport(performer, xformTarget.Coordinates, performer);
        _teleport.Teleport(target, xformPerformer.Coordinates, performer);
    }
}

public sealed partial class SubspaceSwapActionEvent : EntityTargetActionEvent;
