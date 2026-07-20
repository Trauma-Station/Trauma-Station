// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Wizard.Projectile;

namespace Content.Trauma.Shared.Wizard.Projectiles;

public sealed class EntityTrailSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EntityTrailComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<EntityTrailComponent> ent, ref ComponentInit args)
    {
        var (uid, comp) = ent;
        if (!TryComp(uid, out TrailComponent? trail))
            return;

        trail.RenderedEntity = uid;
        Dirty(uid, trail);
    }
}
