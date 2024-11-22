// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.GameTicking.Components;
using Content.Trauma.Server.BloodCult.Gamerule;
using Content.Trauma.Shared.BloodCult.Constructs;

namespace Content.Trauma.Server.BloodCult.Constructs;

/// <summary>
/// Tracks constructs in the blood cult gamerule.
/// </summary>
public sealed partial class ConstructSystem : EntitySystem
{
    // TODO: make event to assign it to a specific cult rule and put this in shared
    [SubscribeLocalEvent]
    private void OnMapInit(Entity<ConstructComponent> ent, ref MapInitEvent args)
    {
        var query = EntityQueryEnumerator<BloodCultRuleComponent, ActiveGameRuleComponent>();
        while (query.MoveNext(out _, out var rule, out _))
        {
            rule.Constructs.Add(ent);
            break;
        }
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<ConstructComponent> ent, ref ComponentShutdown args)
    {
        var query = EntityQueryEnumerator<BloodCultRuleComponent, ActiveGameRuleComponent>();
        while (query.MoveNext(out _, out var rule, out _))
        {
            rule.Constructs.Remove(ent);
            break;
        }
    }
}
