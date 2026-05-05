// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.EntityEffects;
using Content.Shared.Whitelist;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.EntityEffects;

public sealed partial class ReduceActionsCooldown : EntityEffectBase<ReduceActionsCooldown>
{
    [DataField]
    public EntityWhitelist? ActionWhitelist;

    [DataField]
    public bool TargetUser;

    [DataField]
    public TimeSpan MaxReduction = TimeSpan.FromSeconds(5);

    [DataField]
    public float MaxPercentReduction = 0.2f;
}

public sealed partial class ReduceActionsCooldownSystem : EntityEffectSystem<TransformComponent, ReduceActionsCooldown>
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<ReduceActionsCooldown> args)
    {
        var target = args.Effect.TargetUser ? args.User : entity;
        if (target is not { } uid)
            return;

        var now = _timing.CurTime;

        foreach (var action in _actions.GetActions(uid))
        {
            if (!_whitelist.IsWhitelistPass(args.Effect.ActionWhitelist, action))
                continue;

            if (action.Comp.Cooldown is not { } cd)
                continue;

            var curCd = cd.End - now;

            if (curCd <= TimeSpan.Zero)
                continue;

            var reduction = curCd * args.Effect.MaxPercentReduction;
            if (reduction > args.Effect.MaxReduction)
                reduction = args.Effect.MaxReduction;

            _actions.SetCooldown(action.AsNullable(), cd.Start, cd.End - reduction);
        }
    }
}
