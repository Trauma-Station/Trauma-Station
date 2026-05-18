// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text;
using Content.Server.GameTicking.Rules;
using Content.Server.Objectives;
using Content.Trauma.Shared.MobClass;
using Content.Trauma.Shared.Vampires;

namespace Content.Trauma.Server.Vampires.GameTicking;

public sealed partial class VampireRuleSystem : GameRuleSystem<VampireRuleComponent>
{
    [Dependency] private MobClassSystem _mobClass = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireRuleComponent, ObjectivesTextPrependEvent>(OnTextPrepend);
    }

    private void OnTextPrepend(Entity<VampireRuleComponent> ent, ref ObjectivesTextPrependEvent args)
    {
        var bloodConsumed = 0;
        var targetsConsumed = 0;
        var classSelected = "None";

        var query = EntityQueryEnumerator<VampireComponent, VampireBloodsuckingComponent, MobClassComponent>();
        while (query.MoveNext(out var uid, out var comp, out var bloodsucking, out var mobClass))
        {
            bloodConsumed = comp.TotalBlood;
            targetsConsumed = bloodsucking.ConsumedVictims.Count;
            classSelected = _mobClass.GetClassName((uid,  mobClass));
        }

        var sb = new StringBuilder();
        sb.AppendLine($"They consumed a total of {bloodConsumed} units of blood from {targetsConsumed} victims.");
        sb.AppendLine($"They specialized as: {classSelected}.");

        args.Text = sb.ToString();
    }
}
