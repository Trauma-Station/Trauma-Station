// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.Throwing;
using Content.Trauma.Shared.Containers;
using Content.Trauma.Shared.Knowledge.Skills.Components;
using Content.Trauma.Shared.Knowledge.Systems;
using Robust.Shared.Physics.Components;

namespace Content.Trauma.Shared.Knowledge.Skills.Systems;

public sealed class ThrowingSkillSystem : EntitySystem
{
    [Dependency] private readonly SharedKnowledgeSystem _knowledge = default!;

    private static readonly EntProtoId ThrowingKnowledge = "ThrowingKnowledge";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnowledgeHolderComponent, ModifyThrowInsertChanceEvent>(_knowledge.RelayEvent);
        SubscribeLocalEvent<ThrowInsertSkillComponent, ModifyThrowInsertChanceEvent>(OnModifyThrowInsertChance);
        SubscribeLocalEvent<PhysicsComponent, ModifyThrownSpeedEvent>(OnModifyThrowSpeed);
    }

    private void OnModifyThrowInsertChance(Entity<ThrowInsertSkillComponent> ent, ref ModifyThrowInsertChanceEvent args)
    {
        var level = _knowledge.GetLevel(ent);
        args.Chance += ent.Comp.Curve.GetCurve(level);
    }

    public void OnModifyThrowSpeed(Entity<PhysicsComponent> ent, ref ModifyThrownSpeedEvent args)
    {
        var user = args.User;
        var baseThrowSpeed = args.BaseThrowSpeed;

        if (_knowledge.GetContainer(user) is not { } brain)
            return;

        // strength increases speed
        // TODO: Replace with strength call.

        // high throwing skill increases speed
        if (_knowledge.GetSkill(brain, ThrowingKnowledge) is { } throwing &&
            _knowledge.GetMastery(throwing.Comp) > 2)
        {
            baseThrowSpeed *= 1 + 0.2f * _knowledge.SharpCurve(throwing, -50, 50.0f);
        }

        var weight = ent.Comp.FixturesMass;

        // Make it so you gotta throw it further then just at a wall in front.
        _knowledge.AddExperience(brain, ThrowingKnowledge, 1, (int) args.Distance * 5);

        // Make it so you can't just throw a wrapper over and over again.
        // _knowledge.AddExperience(brain, StrengthKnowledge, 1, (int) (weight / 10));

        args.BaseThrowSpeed = baseThrowSpeed;
    }
}
