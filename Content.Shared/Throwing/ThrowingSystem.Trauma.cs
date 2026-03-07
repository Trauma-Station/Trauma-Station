using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.Knowledge.Systems;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Throwing;

public sealed partial class ThrowingSystem
{
    [Dependency] private readonly CommonKnowledgeSystem _knowledge = default!;

    private static readonly EntProtoId StrengthKnowledge = "StrengthKnowledge";
    private static readonly EntProtoId ThrowingKnowledge = "ThrowingKnowledge";

    // TODO: make this shit an event
    public float SkillModifySpeed(EntityUid uid, EntityUid user, float baseThrowSpeedIn, float distanceToTravel)
    {
        var baseThrowSpeed = baseThrowSpeedIn;
        if (!HasComp<KnowledgeHolderComponent>(user))
            return baseThrowSpeed;

        // strength increases speed
        if (_knowledge.GetKnowledge(user, StrengthKnowledge) is { } strength)
        {
            var mastery = _knowledge.GetMastery(strength.Comp);
            if (mastery < 2)
            {
                baseThrowSpeed *= 1 + _knowledge.SharpCurve(strength, 0, 26.0f) / (2.0f);
            }
            else if (mastery > 2)
            {
                baseThrowSpeed *= 1 + 0.5f * _knowledge.SharpCurve(strength, -50, 50.0f);
            }
        }

        // high throwing skill increases speed
        if (_knowledge.GetKnowledge(user, ThrowingKnowledge) is { } throwing &&
            _knowledge.GetMastery(throwing.Comp) > 2)
        {
            baseThrowSpeed *= 1 + 0.2f * _knowledge.SharpCurve(throwing, -50, 50.0f);
        }

        float weight = 0;
        if (TryComp<PhysicsComponent>(uid, out var comp))
            weight = comp.Mass;

        // Make it so you gotta throw it further then just at a wall in front.
        var evThrowing = new AddExperienceEvent(ThrowingKnowledge, 1, (int) distanceToTravel * 5);
        RaiseLocalEvent(user, ref evThrowing);

        // Make it so you can't just throw a wrapper over and over again.
        var evStrength = new AddExperienceEvent(StrengthKnowledge, 1, (int) (weight / 10));
        RaiseLocalEvent(user, ref evStrength);

        return baseThrowSpeed;
    }
}
