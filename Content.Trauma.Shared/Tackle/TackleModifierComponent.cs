using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Tackle;

[RegisterComponent, NetworkedComponent]
public sealed partial class TackleModifierComponent : Component
{
    [DataField]
    public float SpeedMultiplier = 1f;

    [DataField]
    public float RangeMultiplier = 1f;

    [DataField]
    public float KnockdownTimeMultiplier = 1f;

    [DataField]
    public float StaminaCostMultiplier = 1f;

    [DataField]
    public float SpeedModMultiplier = 0.4f;

    [DataField]
    public float MinDistance;

    [DataField]
    public float SkillMod;

    [DataField]
    public bool GrabOnSuccess;

    [DataField]
    public float SeverityModifier = 0.2f;

    [DataField]
    public DamageSpecifier BaseUserDamage = new()
    {
        DamageDict =
        {
            { "Blunt", 20 },
        },
    };

    [DataField]
    public float BaseUserKnockdownTime = 1f;

    [DataField]
    public float BaseTargetStaminaDamage = 22f;

    [DataField]
    public float BaseTargetParalyzeTime = 0.5f;

    [DataField]
    public float BaseTargetKnockdownTime = 2f;
}
