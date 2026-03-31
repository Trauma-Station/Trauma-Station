// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.CCVar;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Trauma.Common.Contests;
using Robust.Shared.Configuration;
using Robust.Shared.Physics.Components;

namespace Content.Trauma.Shared.Contests;

public sealed partial class ContestsSystem : CommonContestsSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfg, GoobCVars.DoContestsSystem, (val) => _doContestSystem = val);
        Subs.CVar(_cfg, GoobCVars.DoMassContests, (val) => _doMassContests = val);
        Subs.CVar(_cfg, GoobCVars.AllowClampOverride, (val) => _allowClampOverride = val);
        Subs.CVar(_cfg, GoobCVars.MassContestsMaxPercentage, (val) => _massContestsMaxPercentage = val);
        Subs.CVar(_cfg, GoobCVars.DoStaminaContests, (val) => _doStaminaContests = val);
        Subs.CVar(_cfg, GoobCVars.DoHealthContests, (val) => _doHealthContests = val);
    }

    /// <summary>
    ///     The presumed average mass of a player entity
    ///     Defaulted to the average mass of an adult human
    /// </summary>
    private const float AverageMass = 71f;
    private bool _doContestSystem;
    private bool _doMassContests;
    private bool _allowClampOverride;
    private float _massContestsMaxPercentage;
    private bool _doStaminaContests;
    private bool _doHealthContests;

    #region Mass Contests
    /// <summary>
    ///     Outputs the ratio of mass between a performer and the average human mass
    /// </summary>
    /// <param name="performerUid">Uid of Performer</param>
    public float MassContest(EntityUid performerUid, bool bypassClamp = false, float rangeFactor = 1f, float otherMass = AverageMass)
    {
        if (_doContestSystem
            || _doMassContests
            || !TryComp<PhysicsComponent>(performerUid, out var performerPhysics)
            || performerPhysics.Mass == 0)
            return 1f;

        return _allowClampOverride && bypassClamp
            ? performerPhysics.Mass / otherMass
            : Math.Clamp(performerPhysics.Mass / otherMass,
                1 - _massContestsMaxPercentage * rangeFactor,
                1 + _massContestsMaxPercentage * rangeFactor);
    }

    /// <inheritdoc cref="MassContest(EntityUid, bool, float, float)"/>
    /// <remarks>
    ///     MaybeMassContest, in case your entity doesn't exist
    /// </remarks>
    public float MassContest(EntityUid? performerUid, bool bypassClamp = false, float rangeFactor = 1f, float otherMass = AverageMass)
    {
        if (_doContestSystem
            || _doMassContests
            || performerUid is null)
            return 1f;

        return MassContest(performerUid.Value, bypassClamp, rangeFactor, otherMass);
    }

    /// <summary>
    ///     Outputs the ratio of mass between a performer and the average human mass
    ///     If a function already has the performer's physics component, this is faster
    /// </summary>
    /// <param name="performerPhysics"></param>
    public float MassContest(PhysicsComponent performerPhysics, bool bypassClamp = false, float rangeFactor = 1f, float otherMass = AverageMass)
    {
        if (_doContestSystem
            || _doMassContests
            || performerPhysics.Mass == 0)
            return 1f;

        return _allowClampOverride && bypassClamp
            ? performerPhysics.Mass / otherMass
            : Math.Clamp(performerPhysics.Mass / otherMass,
                1 - _massContestsMaxPercentage * rangeFactor,
                1 + _massContestsMaxPercentage * rangeFactor);
    }

    /// <summary>
    ///     Outputs the ratio of mass between a performer and a target, accepts either EntityUids or PhysicsComponents in any combination
    ///     If you don't have physics components already in your function, use <see cref="MassContest(EntityUid, float)" /> instead
    /// </summary>
    /// <param name="performerUid"></param>
    /// <param name="targetUid"></param>
    public override float MassContest(PhysicsComponent performerPhysics, PhysicsComponent targetPhysics, bool bypassClamp = false, float rangeFactor = 1f)
    {
        // 1. Unified Guard Clause
        if (_doContestSystem
            || _doMassContests
            || performerPhysics.Mass == 0
            || targetPhysics.InvMass == 0)
            return 1f;

        var ratio = performerPhysics.Mass * targetPhysics.InvMass;

        // 2. Calculated Return
        if (_allowClampOverride && bypassClamp)
            return ratio;

        return Math.Clamp(ratio,
            1 - _massContestsMaxPercentage * rangeFactor,
            1 + _massContestsMaxPercentage * rangeFactor);
    }

    /// <inheritdoc cref="MassContest(PhysicsComponent, PhysicsComponent, bool, float)"/>
    public override float MassContest(EntityUid performerUid, EntityUid targetUid, bool bypassClamp = false, float rangeFactor = 1f)
    {
        if (!TryComp<PhysicsComponent>(performerUid, out var p) || !TryComp<PhysicsComponent>(targetUid, out var t))
            return 1f;

        return MassContest(p, t, bypassClamp, rangeFactor);
    }

    /// <inheritdoc cref="MassContest(PhysicsComponent, PhysicsComponent, bool, float)"/>
    public override float MassContest(EntityUid performerUid, PhysicsComponent targetPhysics, bool bypassClamp = false, float rangeFactor = 1f)
    {
        if (!TryComp<PhysicsComponent>(performerUid, out var p))
            return 1f;

        return MassContest(p, targetPhysics, bypassClamp, rangeFactor);
    }

    /// <inheritdoc cref="MassContest(PhysicsComponent, PhysicsComponent, bool, float)"/>
    public override float MassContest(PhysicsComponent performerPhysics, EntityUid targetUid, bool bypassClamp = false, float rangeFactor = 1f)
    {
        if (!TryComp<PhysicsComponent>(targetUid, out var t))
            return 1f;

        return MassContest(performerPhysics, t, bypassClamp, rangeFactor);
    }

    #endregion
    #region Stamina Contests

    public override float StaminaContest(EntityUid performer, bool bypassClamp = false, float rangeFactor = 1f)
    {
        if (_doContestSystem
            || _doStaminaContests
            || !TryComp<StaminaComponent>(performer, out var perfStamina)
            || perfStamina.StaminaDamage == 0)
            return 1f;

        return _allowClampOverride && bypassClamp
            ? 1 - perfStamina.StaminaDamage / perfStamina.CritThreshold
            : 1 - Math.Clamp(perfStamina.StaminaDamage / perfStamina.CritThreshold, 0, 0.25f * rangeFactor);
    }

    public float StaminaContest(StaminaComponent perfStamina, bool bypassClamp = false, float rangeFactor = 1f)
    {
        if (_doContestSystem
            || _doStaminaContests)
            return 1f;

        return _allowClampOverride && bypassClamp
            ? 1 - perfStamina.StaminaDamage / perfStamina.CritThreshold
            : 1 - Math.Clamp(perfStamina.StaminaDamage / perfStamina.CritThreshold, 0, 0.25f * rangeFactor);
    }

    public override float StaminaContest(EntityUid performer, EntityUid target, bool bypassClamp = false, float rangeFactor = 1f)
    {
        if (_doContestSystem
            || _doStaminaContests
            || !TryComp<StaminaComponent>(performer, out var perfStamina)
            || !TryComp<StaminaComponent>(target, out var targetStamina))
            return 1f;

        return _allowClampOverride && bypassClamp
            ? (1 - perfStamina.StaminaDamage / perfStamina.CritThreshold)
                / (1 - targetStamina.StaminaDamage / targetStamina.CritThreshold)
            : (1 - Math.Clamp(perfStamina.StaminaDamage / perfStamina.CritThreshold, 0, 0.25f * rangeFactor))
                / (1 - Math.Clamp(targetStamina.StaminaDamage / targetStamina.CritThreshold, 0, 0.25f * rangeFactor));
    }

    #endregion

    #region Health Contests

    public override float HealthContest(EntityUid performer, bool bypassClamp = false, float rangeFactor = 1f)
    {
        if (_doContestSystem
            || _doHealthContests
            || !_mobThreshold.TryGetThresholdForState(performer, MobState.Critical, out var threshold))
            return 1f;

        var value = _damage.GetTotalDamage(performer).Float() / threshold.Value.Float();
        return _allowClampOverride && bypassClamp
            ? 1 - value
            : 1 - Math.Clamp(value, 0, 0.25f * rangeFactor);
    }

    public override float HealthContest(EntityUid performer, EntityUid target, bool bypassClamp = false, float rangeFactor = 1f)
    {
        if (_doContestSystem
            || _doHealthContests
            || !_mobThreshold.TryGetThresholdForState(performer, MobState.Critical, out var perfThreshold)
            || !_mobThreshold.TryGetThresholdForState(target, MobState.Critical, out var targetThreshold))
            return 1f;

        var perfValue = _damage.GetTotalDamage(performer).Float() / perfThreshold.Value.Float();
        var targetValue = _damage.GetTotalDamage(target).Float() / targetThreshold.Value.Float();
        return _allowClampOverride && bypassClamp
            ? (1 - perfValue) / (1 - targetValue)
            : (1 - Math.Clamp(perfValue, 0, 0.25f * rangeFactor))
                / (1 - Math.Clamp(targetValue, 0, 0.25f * rangeFactor));
    }
    #endregion
}
