// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;

namespace Content.Trauma.Shared.Body.Organ;

public sealed partial class RenamingOrganSystem : EntitySystem
{
    [Dependency] private MetaDataSystem _meta = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RenamingOrganComponent, OrganGotInsertedEvent>(OnInserted);
    }

    private void OnInserted(Entity<RenamingOrganComponent> ent, ref OrganGotInsertedEvent args)
    {
        _meta.SetEntityName(ent.Owner, Loc.GetString(ent.Comp.Name));
    }
}
