// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.Throwing;
using Content.Trauma.Shared.Containers;
using Content.Trauma.Shared.Knowledge.Components;
using Robust.Shared.Physics.Components;

namespace Content.Trauma.Shared.Knowledge.Systems;

public sealed partial class ThrowingKnowledgeSystem : EntitySystem
{
    [Dependency] private SharedKnowledgeSystem _knowledge = default!;

    private static readonly EntProtoId ThrowingKnowledge = "ThrowingKnowledge";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnowledgeHolderComponent, ModifyThrowInsertChanceEvent>(_knowledge.RelayEvent);
        SubscribeLocalEvent<ThrowInsertKnowledgeComponent, ModifyThrowInsertChanceEvent>(OnModifyThrowInsertChance);
        SubscribeLocalEvent<PhysicsComponent, ModifyThrownSpeedEvent>(OnModifyThrowSpeed);
    }

    private void OnModifyThrowInsertChance(Entity<ThrowInsertKnowledgeComponent> ent, ref ModifyThrowInsertChanceEvent args)
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

        // high throwing skill increases speed
        if (_knowledge.GetKnowledge(brain, ThrowingKnowledge) is { } throwing &&
            _knowledge.GetMastery(throwing.Comp) > 2)
        {
            baseThrowSpeed *= 0.75f * _knowledge.SharpCurve(throwing, 200, 200);
        }

        // Make it so you gotta throw it further then just at a wall in front.
        _knowledge.AddExperience(brain, ThrowingKnowledge, 1, (int) args.Distance * 5);

        args.BaseThrowSpeed = baseThrowSpeed;
    }
}
