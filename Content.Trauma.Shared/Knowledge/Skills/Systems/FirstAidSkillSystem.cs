// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Chemistry;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Shared.Knowledge.Skills.Components;
using Content.Trauma.Shared.Knowledge.Systems;

namespace Content.Trauma.Shared.Knowledge.Skills.Systems;

/// <summary>
/// Handles first aid knowledge interactions.
/// </summary>
public sealed class FirstAidSkillSystem : EntitySystem
{
    [Dependency] private readonly SharedKnowledgeSystem _knowledge = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnowledgeHolderComponent, UserModifyInjectTimeEvent>(_knowledge.RelayEvent);
        SubscribeLocalEvent<InjectTimeSkillComponent, UserModifyInjectTimeEvent>(OnModifyInjectTime);
    }

    private void OnModifyInjectTime(Entity<InjectTimeSkillComponent> ent, ref UserModifyInjectTimeEvent args)
    {
        var level = _knowledge.GetLevel(ent.Owner);
        if (args.Delay > TimeSpan.Zero)
            args.Delay *= ent.Comp.Curve.GetCurve(level);
    }
}
