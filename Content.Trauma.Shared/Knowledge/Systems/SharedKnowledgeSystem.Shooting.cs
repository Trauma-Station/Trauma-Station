// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Projectiles;
using Content.Trauma.Common.Knowledge;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Knowledge.Systems;

public abstract partial class SharedKnowledgeSystem
{
    private static readonly EntProtoId ShootingKnowledge = "ShootingKnowledge";

    private void InitializeShooting()
    {
        SubscribeLocalEvent<ProjectileComponent, ProjectileHitEvent>(DealShootingExperience);
    }

    private void DealShootingExperience(Entity<ProjectileComponent> ent, ref ProjectileHitEvent args)
    {
        if (args.Shooter is not { } shooter || !_mobState.IsAlive(args.Target))
            return;

        // TODO: higher caliber has higher limit
        var ev = new AddExperienceEvent(ShootingKnowledge, 1, 10);
        RaiseLocalEvent(shooter, ref ev);
    }
}
