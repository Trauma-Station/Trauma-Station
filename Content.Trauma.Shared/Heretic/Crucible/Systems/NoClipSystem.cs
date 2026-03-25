// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Robust.Shared.Physics.Events;

namespace Content.Trauma.Shared.Heretic.Crucible.Systems;

public sealed class NoClipSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Components.NoClipComponent, PreventCollideEvent>(OnPreventCollide);
        SubscribeLocalEvent<Components.NoClipComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<Components.NoClipComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.ExamineLoc is { } loc)
            args.PushMarkup(Loc.GetString(loc, ("ent", Identity.Entity(ent, EntityManager))));
    }

    private void OnPreventCollide(Entity<Components.NoClipComponent> ent, ref PreventCollideEvent args)
    {
        if (!args.OtherFixture.Hard)
            return;

        args.Cancelled = true;
    }
}
