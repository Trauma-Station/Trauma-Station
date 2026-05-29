// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Trauma.Shared.Heretic.Components.Ghoul;
using Content.Trauma.Shared.Heretic.Systems.Abilities;

namespace Content.Trauma.Shared.Heretic.EntityEffects;

public sealed partial class CreateFleshMimic : EntityEffectBase<CreateFleshMimic>
{
    [DataField]
    public bool GiveBlade;

    [DataField]
    public bool MakeGhostRole = true;

    [DataField]
    public FixedPoint2 Health = 50;
}

public sealed partial class CreateFleshMimicEffectSystem : EntityEffectSystem<MobStateComponent, CreateFleshMimic>
{
    [Dependency] private SharedHereticAbilitySystem _ability = default!;
    [Dependency] private SharedMindSystem _mind = default!;

    protected override void Effect(Entity<MobStateComponent> entity, ref EntityEffectEvent<CreateFleshMimic> args)
    {
        if (args.User is not { } user || !_mind.TryGetMind(user, out var userMind, out _))
            return;

        _ability.CreateFleshMimic(entity,
            user,
            GetNetEntity(userMind).Id,
            args.Effect.GiveBlade,
            args.Effect.MakeGhostRole,
            args.Effect.Health,
            null,
            true);
    }
}
