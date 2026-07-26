// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Weapons.Melee.Events;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Shared.Knowledge.Components;

namespace Content.Trauma.Shared.Knowledge.Systems;

public sealed partial class MeleeKnowledgeSystem : EntitySystem
{
    [Dependency] private SharedKnowledgeSystem _knowledge = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnowledgeHolderComponent, GetUserMeleeDamageEvent>(_knowledge.RelayActiveEvent);
    }

    [SubscribeLocalEvent]
    private void OnGetMeleeAttackRate(Entity<MeleeSpeedKnowledgeComponent> ent, ref GetMeleeAttackRateEvent args)
    {
        var level = _knowledge.GetLevel(ent.Owner);
        args.Multipliers *= ent.Comp.Curve.GetCurve(level);
    }

    [SubscribeLocalEvent]
    private void OnGetMeleeDamage(Entity<MeleeDamageKnowledgeComponent> ent, ref GetUserMeleeDamageEvent args)
    {
        var level = _knowledge.GetLevel(ent.Owner);
        args.Damage *= ent.Comp.Curve.GetCurve(level);
    }
}
