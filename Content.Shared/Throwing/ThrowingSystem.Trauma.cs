using Content.Shared.Weapons.Ranged.Systems;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.Knowledge.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Throwing;

public sealed partial class ThrowingSystem
{
    [Dependency] private readonly CommonKnowledgeSystem _knowledge = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;

    private static readonly EntProtoId StrengthKnowledge = "StrengthKnowledge";
    private static readonly EntProtoId ThrowingKnowledge = "ThrowingKnowledge";

    public (float, float) RandomSkillThrowingAngle(EntityUid user, float baseThrowSpeedIn)
    {
        var baseThrowSpeed = baseThrowSpeedIn;
        var throwingRandomness = 0.0f;
        if (!HasComp<KnowledgeHolderComponent>(user))
            return (baseThrowSpeed, throwingRandomness);
        if (_knowledge.TryGetKnowledgeUnit(user, StrengthKnowledge) is { } strength)
        {
            if (_knowledge.GetMastery(strength) < 2)
            {
                baseThrowSpeed *= 0.5f + _knowledge.SharpCurve(strength, 0, 26.0f) / (2.0f);
            }
            else if (_knowledge.GetMastery(strength) > 2)
            {
                baseThrowSpeed *= 1 + 0.5f * _knowledge.SharpCurve(strength, -50, 50.0f);
            }
        }
        else
            baseThrowSpeed *= 0.5f;
        if (_knowledge.TryGetKnowledgeUnit(user, ThrowingKnowledge) is { } throwing)
        {
            if (_knowledge.GetMastery(throwing) < 2)
            {
                throwingRandomness = 1.0f - _knowledge.SharpCurve(throwing, 0, 26.0f);
                throwingRandomness *= _gun.Random(user).NextFloat(-0.5f, 0.5f) * 3.14159f;
            }
            else if (_knowledge.GetMastery(throwing) > 2)
            {
                baseThrowSpeed *= 1 + 0.2f * _knowledge.SharpCurve(throwing, -50, 50.0f);
            }
        }
        else
            throwingRandomness = _gun.Random(user).NextFloat(-0.5f, 0.5f) * 3.14159f;
        var evThrowing = new AddExperienceEvent(ThrowingKnowledge, 1);
        RaiseLocalEvent(user, ref evThrowing);
        var evStrength = new AddExperienceEvent(StrengthKnowledge, 1);
        RaiseLocalEvent(user, ref evStrength);

        return (baseThrowSpeed, throwingRandomness);
    }
}
