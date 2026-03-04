// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Text;
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

        var ev = new AddExperienceEvent(ShootingKnowledge, 1);
        RaiseLocalEvent(shooter, ref ev);
    }
}
