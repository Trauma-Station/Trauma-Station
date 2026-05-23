// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Content.Shared.Gibbing;

namespace Content.Medical.Shared.Body;

public sealed partial class FragileOrganSystem : EntitySystem
{
    [Dependency] private GibbingSystem _gibbing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FragileOrganComponent, OrganGotRemovedEvent>(OnRemove,
            after: new[] {typeof(BodyPartSystem)});
    }

    private void OnRemove(Entity<FragileOrganComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        _gibbing.Gib(ent.Owner);
    }
}
