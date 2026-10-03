// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.EntityEffects;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Entity effect that starts a gamerule.
/// Target entity just has to exist, it doesn't have any impact on anything.
/// </summary>
public sealed partial class StartGameRule : EntityEffectBase<StartGameRule>
{
    [DataField(required: true)]
    public EntProtoId<GameRuleComponent> Rule;
}

public sealed partial class StartGameRuleSystem : EntityEffectSystem<MetaDataComponent, StartGameRule>
{
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private ISharedAdminLogManager _adminLog = default!;

    protected override void Effect(Entity<MetaDataComponent> ent, ref EntityEffectEvent<StartGameRule> args)
    {
        var rule = args.Effect.Rule;
        _ticker.StartGameRule(rule);
        if (args.User is {} user)
            _adminLog.Add(LogType.EventStarted, LogImpact.High, $"{user:player} caused gamerule {rule} to be started via entity effect on {ent.Owner:target}");
    }
}
