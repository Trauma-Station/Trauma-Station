using Content.Shared.Pulling.Events;

namespace Content.Trauma.Shared.PullBlocker;

public sealed class BlockPullSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<BlockPullComponent, BeingPulledAttemptEvent>(OnAttemptPull);
    }

    private void OnAttemptPull(Entity<BlockPullComponent> ent, ref BeingPulledAttemptEvent args)
    {
        args.Cancel();
    }
}
