using Content.Shared.Chemistry.Components;
using Content.Shared.EntityEffects;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.EntityEffects.Effects;

/// <summary>
/// Default metabolism for stimulants and tranqs. Attempts to find a MovementSpeedModifier on the target,
/// adding one if not there and to change the movespeed
/// Trauma - moved it here out of core files and refactored (TODO: make this a status effect? fucking shitcode)
/// </summary>
public sealed partial class NitriumMovespeedModifier : EntityEffectBase<NitriumMovespeedModifier>
{
    /// <summary>
    /// How much the entities' walk speed is multiplied by.
    /// </summary>
    [DataField]
    public float WalkSpeedModifier = 1f;

    /// <summary>
    /// How much the entities' run speed is multiplied by.
    /// </summary>
    [DataField]
    public float SprintSpeedModifier = 1f;

    /// <summary>
    /// How long the modifier refreshes for (in seconds).
    /// Is scaled by reagent amount if used with an EntityEffectReagentArgs.
    /// </summary>
    [DataField]
    public TimeSpan StatusLifetime = TimeSpan.FromSeconds(6f);

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-movespeed-modifier",
            ("chance", Probability),
            ("walkspeed", WalkSpeedModifier),
            ("sprintspeed", SprintSpeedModifier),
            ("time", StatusLifetime.TotalSeconds));
}

/// <summary>
/// Remove reagent at set rate, changes the movespeed modifiers and adds a MovespeedModifierMetabolismComponent if not already there.
/// </summary>
public sealed class NitriumMovespeedModifierEffectSystem : EntityEffectSystem<InputMoverComponent, NitriumMovespeedModifier>
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _modifier = default!;

    protected override void Effect(Entity<InputMoverComponent> ent, ref EntityEffectEvent<NitriumMovespeedModifier> args)
    {
        var status = EnsureComp<MovespeedModifierMetabolismComponent>(ent);

        // Only refresh movement if we need to.
        var walk = args.Effect.WalkSpeedModifier;
        var sprint = args.Effect.SprintSpeedModifier;
        var modified = !status.WalkSpeedModifier.Equals(walk) ||
                       !status.SprintSpeedModifier.Equals(sprint);

        status.WalkSpeedModifier = walk;
        status.SprintSpeedModifier = sprint;

        SetTimer((ent.Owner, status), args.Effect.StatusLifetime);

        if (modified)
            _modifier.RefreshMovementSpeedModifiers(ent.Owner);
    }

    public void SetTimer(Entity<MovespeedModifierMetabolismComponent> status, TimeSpan time)
    {
        status.Comp.ModifierTimer = _timing.CurTime + time;
        Dirty(status);
    }
}
