// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Knowledge.Components;

namespace Content.Trauma.Shared.Knowledge.Systems;

public sealed partial class KnowledgeConflictSystem : EntitySystem
{
    [Dependency] private SharedKnowledgeSystem _knowledge = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnowledgeConflictComponent, KnowledgeAddedEvent>(OnAdded);
    }

    private void OnAdded(Entity<KnowledgeConflictComponent> ent, ref KnowledgeAddedEvent args)
    {
        foreach (var conflict in ent.Comp.Conflicts)
        {
            _knowledge.RemoveKnowledge(args.Holder, conflict, force: true);
        }
    }
}
