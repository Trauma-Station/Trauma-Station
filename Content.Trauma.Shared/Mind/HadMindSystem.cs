// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Mind.Components;

namespace Content.Trauma.Shared.Mind;

public sealed class HadMindSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindContainerComponent, MindAddedMessage>(OnAdded);
    }

    private void OnAdded(Entity<MindContainerComponent> ent, ref MindAddedMessage args)
    {
        ent.Comp.HadMind = true;
        ent.Comp.OldMind = args.Mind;
        Dirty(ent);
    }
}
