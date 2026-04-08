// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Events;
using Content.Trauma.Common.Damage;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Shared.Knowledge.Skills.Components;
using Content.Trauma.Shared.Knowledge.Systems;

namespace Content.Trauma.Shared.Knowledge.Skills.Systems;

public sealed class StaminaSkillSystem : EntitySystem
{
    [Dependency] private readonly SharedKnowledgeSystem _knowledge = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnowledgeHolderComponent, BeforeStaminaDamageEvent>(_knowledge.RelayEvent);
        SubscribeLocalEvent<KnowledgeHolderComponent, TookStaminaDamageEvent>(_knowledge.RelayEvent);
        SubscribeLocalEvent<StaminaSkillComponent, BeforeStaminaDamageEvent>(OnBeforeStaminaDamage);
        SubscribeLocalEvent<StaminaSkillComponent, TookStaminaDamageEvent>(OnTookStaminaDamage);
    }

    private void OnBeforeStaminaDamage(Entity<StaminaSkillComponent> ent, ref BeforeStaminaDamageEvent args)
    {
        var level = _knowledge.GetLevel(ent.Owner);
        args.Value *= ent.Comp.Curve.GetCurve(level);
    }

    private void OnTookStaminaDamage(Entity<StaminaSkillComponent> ent, ref TookStaminaDamageEvent args)
    {
        var xp = Math.Min((int) args.Amount / ent.Comp.DamageScale, ent.Comp.MaxGain);
        // TODO: better limit
        var limit = 60;
        _knowledge.AddExperience(ent.Owner, args.Target, xp, limit);
    }
}
