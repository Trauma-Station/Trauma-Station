// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.GameTicking;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.EntityEffects;
using Content.Trauma.Shared.EntityEffects;

namespace Content.Trauma.Server.EntityEffects;

public sealed partial class StartGameRuleSystem : EntityEffectSystem<MetaDataComponent, StartGameRule>
{
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private ISharedAdminLogManager _adminLog = default!;

    protected override void Effect(Entity<MetaDataComponent> ent, ref EntityEffectEvent<StartGameRule> args)
    {
        var rule = args.Effect.Rule;
        _ticker.StartGameRule(rule);
        if (args.User is {} user)
            _adminLog.Add(LogType.EventStarted, LogImpact.High, $"{ToPrettyString(user):player} caused gamerule {rule} to be started via entity effect on {ToPrettyString(ent.AsNullable())}");
    }
}
