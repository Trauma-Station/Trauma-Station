// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Weapons.Ranged;
using Content.Shared.Mobs.Systems;
using Content.Shared.Projectiles;
using Content.Trauma.Common.Knowledge.Components;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Knowledge.Systems;
public sealed class ShootingKnowledgeSystem : EntitySystem
{
    [Dependency] private readonly SharedKnowledgeSystem _knowledge = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    private static readonly EntProtoId ShootingKnowledge = "ShootingKnowledge";
    private static readonly EntProtoId WeaponsKnowledge = "WeaponsKnowledge";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnowledgeHolderComponent, AmmoShotUserEvent>(OnAddShootingExperience);
        SubscribeLocalEvent<ProjectileComponent, ProjectileHitEvent>(OnHitShootingExperience);
    }

    private void OnAddShootingExperience(Entity<KnowledgeHolderComponent> ent, ref AmmoShotUserEvent args)
    {
        if (_knowledge.GetContainer(ent.Owner) is not { } brain)
            return;
        // TODO: scale it based on the gun, pistols are easier to shoot than railguns
        _knowledge.AddExperience(brain, ShootingKnowledge, 1, 20);
        _knowledge.AddExperience(brain, WeaponsKnowledge, 1, 20);
    }

    private void OnHitShootingExperience(Entity<ProjectileComponent> ent, ref ProjectileHitEvent args)
    {
        if (args.Shooter is not { } shooter || _knowledge.GetContainer(shooter) is not { } brain || !_mobState.IsAlive(args.Target))
            return;

        // TODO: higher caliber has higher limit
        _knowledge.AddExperience(brain, ShootingKnowledge, 1, 10);
    }
}
