// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Trauma.Shared.EntityEffects.Effects;
using Content.Trauma.Server.GrantTemporatyComponent;

namespace Content.Trauma.Server.EntityEffects.Effects;

public sealed partial class GrantTemporatyComponentEffectSystem : EntityEffectSystem<TransformComponent, GrantTemporatyComponentEffect>
{
    [Dependency] private GrantTemporatyComponentSystem _grantSys = default!;

    protected override void Effect(Entity<TransformComponent> ent, ref EntityEffectEvent<GrantTemporatyComponentEffect> args)
    {
        _grantSys.GrantTemporatyComponent(ent, args.Effect.Components, args.Effect.LifeSpan);
    }
}