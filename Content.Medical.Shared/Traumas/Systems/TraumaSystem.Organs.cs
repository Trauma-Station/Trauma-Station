using System.Linq;
using Content.Medical.Common.Body;
using Content.Medical.Common.CCVar;
using Content.Medical.Common.Traumas;
using Content.Medical.Shared.Pain;
using Content.Medical.Shared.Wounds;
using Content.Shared.Body;
using Content.Shared.StatusEffectNew;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Medical.Shared.Traumas;

public partial class TraumaSystem
{
    [Dependency] private readonly StatusEffectsSystem _status = default!;

    private const string OrganDamagePainIdentifier = "OrganDamage";

    private float _organSlowdownMul;

    public static readonly EntProtoId OrganSlowdownStatusEffect = "OrganSlowdownStatusEffect";

    private void InitOrgans()
    {
        SubscribeLocalEvent<WoundableComponent, OrganIntegrityChangedEventOnWoundable>(OnOrganIntegrityOnWoundableChanged);
        SubscribeLocalEvent<InternalOrganComponent, OrganIntegrityChangedEvent>(OnOrganIntegrityChanged);
        SubscribeLocalEvent<WoundableComponent, OrganDamageSeverityChangedOnWoundable>(OnOrganSeverityChanged);

        Subs.CVar(_cfg, SurgeryCVars.OrganTraumaSlowdownTimeMultiplier, x => _organSlowdownMul = x, true);
    }

    #region Event handling

    private void OnOrganIntegrityOnWoundableChanged(Entity<WoundableComponent> bodyPart, ref OrganIntegrityChangedEventOnWoundable args)
    {
        if (_body.GetBody(bodyPart.Owner) is not {} body)
            return;

        if (!_consciousness.TryGetNerveSystem(body, out var nerveSys))
            return;

        var organs = _body.GetOrgans<InternalOrganComponent>(body);
        var totalIntegrity = FixedPoint2.Zero;
        var totalIntegrityCap = FixedPoint2.Zero;
        foreach (var organ in organs)
        {
            totalIntegrity += organ.Comp.OrganIntegrity;
            totalIntegrityCap += organ.Comp.IntegrityCap;
        }

        // Getting your organ turned into a blood mush inside you applies a LOT of internal pain, that can get you dead.
        _pain.ChangePainModifier(
            nerveSys.Value.AsNullable(),
            bodyPart.Owner,
            OrganDamagePainIdentifier,
            (totalIntegrityCap - totalIntegrity) / 2);
    }

    private void OnOrganIntegrityChanged(Entity<InternalOrganComponent> organ, ref OrganIntegrityChangedEvent args)
    {
        if (_body.GetBody(organ.Owner) is not {} body)
            return;

        if (args.NewIntegrity < organ.Comp.IntegrityCap)
            return;

        foreach (var trauma in GetBodyTraumas(body, TraumaType.OrganDamage))
        {
            if (trauma.Comp.TraumaTarget == organ)
                RemoveTrauma(trauma);
        }
    }

    private void OnOrganSeverityChanged(Entity<WoundableComponent> bodyPart, ref OrganDamageSeverityChangedOnWoundable args)
    {
        if (_body.GetBody(bodyPart.Owner) is not {} body
            || args.NewSeverity < args.OldSeverity)
            return;

        _popup.PopupClient(Loc.GetString($"popup-trauma-OrganDamage-{args.NewSeverity.ToString()}", ("part", bodyPart)),
            body,
            body,
            PopupType.SmallCaution);

        if (args.NewSeverity != OrganSeverity.Destroyed)
            return;

        if (_consciousness.TryGetNerveSystem(body, out var nerveSys)
            && !_mobState.IsDead(body))
        {
            var sex = Sex.Unsexed;
            if (TryComp<HumanoidProfileComponent>(body, out var humanoid))
                sex = humanoid.Sex;

            var brain = nerveSys.Value;
            _pain.PlayPainSoundWithCleanup(
                body,
                brain.Comp,
                brain.Comp.OrganDestructionReflexSounds[sex],
                AudioParams.Default.WithVolume(6f));

            _stun.TryUpdateParalyzeDuration(body, brain.Comp.OrganDamageStunTime);
            var slowdown = brain.Comp.OrganDamageStunTime * _organSlowdownMul;
            _status.TryUpdateStatusEffectDuration(body, OrganSlowdownStatusEffect, slowdown);
        }

        if (TryGetWoundableTrauma(bodyPart, out var traumas, TraumaType.OrganDamage, bodyPart))
        {
            foreach (var trauma in traumas)
            {
                if (trauma.Comp.TraumaTarget != args.Organ)
                    continue;

                RemoveTrauma(trauma);
            }
        }

        _audio.PlayPvs(args.Organ.Comp.OrganDestroyedSound, body);
        _part.RemoveOrgan(bodyPart.Owner, args.Organ.Owner);
        PredictedQueueDel(args.Organ);
    }

    #endregion

    #region Public API
    public bool TryCreateOrganDamageModifier(EntityUid uid,
        FixedPoint2 severity,
        EntityUid effectOwner,
        string identifier,
        InternalOrganComponent? organ = null)
    {
        if (severity == 0
            || !Resolve(uid, ref organ))
            return false;

        if (!organ.IntegrityModifiers.TryAdd((identifier, effectOwner), severity))
            return false;

        UpdateOrganIntegrity(uid, organ);

        return true;
    }

    public bool TrySetOrganDamageModifier(EntityUid uid,
        FixedPoint2 severity,
        EntityUid effectOwner,
        string identifier,
        InternalOrganComponent? organ = null)
    {
        if (severity == 0
            || !Resolve(uid, ref organ))
            return false;

        organ.IntegrityModifiers[(identifier, effectOwner)] = severity;
        UpdateOrganIntegrity(uid, organ);

        return true;
    }

    public bool TryChangeOrganDamageModifier(EntityUid uid,
        FixedPoint2 change,
        EntityUid effectOwner,
        string identifier,
        InternalOrganComponent? organ = null)
    {
        if (change == 0
            || !Resolve(uid, ref organ))
            return false;

        if (!organ.IntegrityModifiers.TryGetValue((identifier, effectOwner), out var value))
            return false;

        organ.IntegrityModifiers[(identifier, effectOwner)] = value + change;
        UpdateOrganIntegrity(uid, organ);

        return true;
    }

    public bool TryRemoveOrganDamageModifier(EntityUid uid,
        EntityUid effectOwner,
        string identifier,
        InternalOrganComponent? organ = null)
    {
        if (!Resolve(uid, ref organ))
            return false;

        if (!organ.IntegrityModifiers.Remove((identifier, effectOwner)))
            return false;

        if (TryComp<TraumaComponent>(effectOwner, out var traumaComp))
            RemoveTrauma((effectOwner, traumaComp));

        UpdateOrganIntegrity(uid, organ);
        return true;
    }

    #endregion

    #region Private API

    private void UpdateOrganIntegrity(EntityUid uid, InternalOrganComponent organ)
    {
        var oldIntegrity = organ.OrganIntegrity;

        if (organ.IntegrityModifiers.Count > 0)
            organ.OrganIntegrity = FixedPoint2.Clamp(organ.IntegrityModifiers
                .Aggregate(FixedPoint2.Zero, (current, modifier) => current + modifier.Value),
                0,
                organ.IntegrityCap);

        if (oldIntegrity != organ.OrganIntegrity)
        {
            var ev = new OrganIntegrityChangedEvent(oldIntegrity, organ.OrganIntegrity);
            RaiseLocalEvent(uid, ref ev);

            if (_container.TryGetContainingContainer((uid, Transform(uid), MetaData(uid)), out var container))
            {
                var ev1 = new OrganIntegrityChangedEventOnWoundable((uid, organ), oldIntegrity, organ.OrganIntegrity);
                RaiseLocalEvent(container.Owner, ref ev1);
            }
        }

        var nearestSeverity = organ.OrganSeverity;
        foreach (var (severity, value) in organ.IntegrityThresholds.OrderByDescending(kv => kv.Value))
        {
            if (organ.OrganIntegrity > value)
                continue;

            nearestSeverity = severity;
            break;
        }

        if (nearestSeverity != organ.OrganSeverity)
        {
            var ev = new OrganDamageSeverityChanged(organ.OrganSeverity, nearestSeverity);
            RaiseLocalEvent(uid, ref ev);
            if (_container.TryGetContainingContainer((uid, Transform(uid), MetaData(uid)), out var container))
            {
                var ev1 = new OrganDamageSeverityChangedOnWoundable((uid, organ), organ.OrganSeverity, nearestSeverity);
                RaiseLocalEvent(container.Owner, ref ev1);
            }
        }

        organ.OrganSeverity = nearestSeverity;
        Dirty(uid, organ);
    }

    #endregion
}
