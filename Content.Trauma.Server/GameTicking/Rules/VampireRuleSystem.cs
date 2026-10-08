// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text;
using Content.Server.Objectives;
using Content.Shared.GameTicking.Rules;
using Content.Shared.Mind;
using Content.Trauma.Common.Vampires;
using Content.Trauma.Shared.GameTicking.Rules;
using Content.Trauma.Shared.MobClass;
using Content.Trauma.Shared.Vampires;

namespace Content.Trauma.Server.GameTicking.Rules;

public sealed partial class VampireRuleSystem : GameRuleSystem<VampireRuleComponent>
{
    [Dependency] private MobClassSystem _mobClass = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private ObjectivesSystem _objective = default!;

    [SubscribeLocalEvent]
    private void OnTextPrepend(Entity<VampireRuleComponent> ent, ref ObjectivesTextPrependEvent args)
    {
        var sb = new StringBuilder();

        var query = EntityQueryEnumerator<VampireComponent, VampireBloodsuckingComponent, MobClassComponent>();
        while (query.MoveNext(out var uid, out var comp, out var bloodsucking, out var mobClass))
        {
            if (!_mind.TryGetMind(uid, out var mindId, out var mind))
                continue;

            var classSelected = _mobClass.GetClassName((uid,  mobClass));
            var name = _objective.GetTitle((mindId, mind), Name(mind.OwnedEntity ?? mindId));

            sb.AppendLine($"{name} consumed a total of [color=red]{comp.TotalBlood}[/color] units of blood from {bloodsucking.ConsumedVictims.Count} victims.");
            sb.AppendLine($"{name} specialized as: {classSelected}.");
        }

        args.Text = sb.ToString();
    }
}
