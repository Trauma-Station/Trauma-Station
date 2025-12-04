// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Paper;
using Content.Shared.UserInterface;

namespace Content.Trauma.Shared.Paper;

public sealed class BlockReadingSystem : EntitySystem
{
    private EntityQuery<BlockReadingComponent> _query;

    public override void Initialize()
    {
        base.Initialize();

        _query = GetEntityQuery<BlockReadingComponent>();

        SubscribeLocalEvent<PaperComponent, ActivatableUIOpenAttemptEvent>(OnOpenAttempt);
    }

    private void OnOpenAttempt(Entity<PaperComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (_query.HasComp(args.User))
            args.Cancel();
    }
}
