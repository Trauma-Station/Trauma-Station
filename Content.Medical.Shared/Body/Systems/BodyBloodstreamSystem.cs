// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Body;
using Content.Medical.Common.CCVar;
using Content.Medical.Shared.Wounds;
using Content.Medical.Shared.Traumas;
using Content.Shared.Alert;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Medical.Shared.Body;

public sealed partial class BodyBloodstreamSystem : EntitySystem
{
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private WoundSystem _wound = default!;
    [Dependency] private EntityQuery<BleedInflicterComponent> _bleedQuery = default!;
    [Dependency] private EntityQuery<WoundableComponent> _woundableQuery = default!;

    private float _bleedingSeverity = 1f;
    private float _bleedScaleTime = 1f;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfg, SurgeryCVars.BleedingSeverityTrade, x => _bleedingSeverity = x, true);
        Subs.CVar(_cfg, SurgeryCVars.BleedsScalingTime, x => _bleedScaleTime = x, true);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var bleedsQuery = EntityQueryEnumerator<BleedInflicterComponent>();
        var now = _timing.CurTime;
        while (bleedsQuery.MoveNext(out var ent, out var bleeds))
        {
            var bleeding = bleeds.BleedingAmount > 0 && CanWoundBleed((ent, bleeds));
            if (bleeding != bleeds.IsBleeding)
            {
                bleeds.IsBleeding = bleeding;
                DirtyField(ent, bleeds, nameof(BleedInflicterComponent.IsBleeding));
            }

            if (!bleeds.IsBleeding)
                continue;

            var totalTime = bleeds.ScalingFinishesAt - bleeds.ScalingStartsAt;
            var currentTime = bleeds.ScalingFinishesAt - now;

            if (totalTime <= currentTime || bleeds.ScalingLimit >= bleeds.Scaling)
                continue;

            var newBleeds = FixedPoint2.Clamp(
                (totalTime / currentTime) / (bleeds.ScalingLimit - bleeds.Scaling),
                0,
                bleeds.ScalingLimit);

            if (bleeds.Scaling == newBleeds)
                continue;

            bleeds.Scaling = newBleeds;
            DirtyField(ent, bleeds, nameof(BleedInflicterComponent.Scaling));
        }
    }

    /// <summary>
    /// Add a bleed-ability modifier on woundable
    /// </summary>
    /// <param name="part">The bodypart to apply the modifiers</param>
    /// <param name="identifier">string identifier of the modifier</param>
    /// <param name="priority">Priority of the said modifier</param>
    /// <param name="canBleed">Should the wounds bleed?</param>
    /// <param name="force">If forced, won't stop after failing to apply one modifier</param>
    /// <returns>Return true if applied</returns>
    public bool TryAddPartBleedModifier(
        Entity<WoundableComponent?> part,
        string identifier,
        int priority,
        bool canBleed,
        bool force = false)
    {
        foreach (var wound in _wound.GetWoundableWounds(part))
        {
            if (!_bleedQuery.TryComp(wound, out var bleeds))
                continue;

            if (TryAddBleedModifier((wound, bleeds), identifier, priority, canBleed))
                continue;

            if (!force)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Add a bleed-ability modifier
    /// </summary>
    /// <param name="ent">The wound</param>
    /// <param name="identifier">string identifier of the modifier</param>
    /// <param name="priority">Priority of the said modifier</param>
    /// <param name="canBleed">Should the wound bleed?</param>
    /// <returns>Return true if applied</returns>
    public bool TryAddBleedModifier(
        Entity<BleedInflicterComponent?> ent,
        string identifier,
        int priority,
        bool canBleed)
    {
        if (!_bleedQuery.Resolve(ent, ref ent.Comp))
            return false;

        if (!ent.Comp.BleedingModifiers.TryAdd(identifier, (priority, canBleed)))
            return false;

        DirtyField(ent, ent.Comp, nameof(BleedInflicterComponent.BleedingModifiers));
        return true;
    }

    /// <summary>
    /// Remove a bleed-ability modifier from a woundable
    /// </summary>
    /// <param name="part">The bodypart</param>
    /// <param name="identifier">string identifier of the modifier</param>
    /// <param name="force">If forced, won't stop applying modifiers after failing one wound</param>
    /// <returns>Returns true if removed all modifiers ON WOUNDABLE</returns>
    public bool TryRemoveBleedModifier(
        Entity<WoundableComponent?> part,
        string identifier,
        bool force = false)
    {
        foreach (var wound in _wound.GetWoundableWounds(part))
        {
            if (!_bleedQuery.TryComp(wound, out var bleeds))
                continue;

            if (TryRemoveBleedModifier((wound, bleeds), identifier))
                continue;

            if (!force)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Remove a bleed-ability modifier
    /// </summary>
    /// <param name="ent">The wound</param>
    /// <param name="identifier">string identifier of the modifier</param>
    /// <returns>Return true if removed</returns>
    public bool TryRemoveBleedModifier(
        Entity<BleedInflicterComponent?> ent,
        string identifier)
    {
        if (!_bleedQuery.Resolve(ent, ref ent.Comp))
            return false;

        if (!ent.Comp.BleedingModifiers.Remove(identifier))
            return false;

        DirtyField(ent, ent.Comp, nameof(BleedInflicterComponent.BleedingModifiers));
        return true;
    }

    /// <summary>
    /// Self-explanatory
    /// </summary>
    /// <returns>Returns whether if the wound can bleed</returns>
    public bool CanWoundBleed(Entity<BleedInflicterComponent?> ent)
    {
        if (!_bleedQuery.Resolve(ent, ref ent.Comp))
            return false;

        var nearestModifier = ent.Comp.BleedingModifiers.FirstOrNull();
        if (nearestModifier == null)
            return true; // No modifiers. return true

        var lastCanBleed = true;
        var lastPriority = 0;
        foreach (var (_, pair) in ent.Comp.BleedingModifiers)
        {
            if (pair.Priority <= lastPriority)
                continue;

            lastPriority = pair.Priority;
            lastCanBleed = pair.CanBleed;
        }

        return lastCanBleed;
    }

    [SubscribeLocalEvent]
    private void OnWoundAdded(EntityUid uid, BleedInflicterComponent component, ref WoundAddedEvent args)
    {
        if (!args.Woundable.CanBleed ||
            !CanWoundBleed((uid, component)) ||
            args.Component.WoundSeverityPoint < component.SeverityThreshold)
            return;

        // wounds that BLEED will not HEAL.
        // wounds that bleed. will you heal them, to me?
        component.BleedingAmountRaw = args.Component.WoundSeverityPoint * _bleedingSeverity;

        var formula = (float) (args.Component.WoundSeverityPoint / _bleedScaleTime * component.ScalingSpeed);
        component.ScalingFinishesAt = _timing.CurTime + TimeSpan.FromSeconds(formula);
        component.ScalingStartsAt = _timing.CurTime;
        component.IsBleeding = true;

        Dirty(uid, component);

        if (_body.GetBody(args.Component.HoldingWoundable) is { } body)
            _bloodstream.TryModifyBleedAmount(body, component.BleedingAmountRaw.Float());
    }

    [SubscribeLocalEvent]
    private void OnWoundHealAttempt(EntityUid uid, BleedInflicterComponent component, ref WoundHealAttemptEvent args)
    {
        if (args.IgnoreBlockers)
            return;

        if (component.IsBleeding)
            args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnBleedInflicterSeverityUpdate(EntityUid uid,
        BleedInflicterComponent component,
        ref WoundSeverityPointChangedEvent args)
    {
        if (!CanWoundBleed((uid, component))
            || !_woundableQuery.TryComp(args.Component.HoldingWoundable, out var woundable)
            || !woundable.CanBleed
            || args.NewSeverity < component.SeverityThreshold
            || args.NewSeverity < args.OldSeverity)
            return;

        var oldBleedsAmount = args.OldSeverity * _bleedingSeverity;
        component.BleedingAmountRaw = args.NewSeverity * _bleedingSeverity;

        var severityPenalty = component.BleedingAmountRaw - oldBleedsAmount / _bleedScaleTime;
        component.SeverityPenalty += severityPenalty;

        var formula = (float) (args.NewSeverity / _bleedScaleTime * component.ScalingSpeed);
        component.ScalingFinishesAt = _timing.CurTime + TimeSpan.FromSeconds(formula);
        component.ScalingStartsAt = _timing.CurTime;

        if (!component.IsBleeding)
        {
            component.ScalingLimit += 0.6;
            component.IsBleeding = true;
            // When bleeding is reopened, the severity is increased
        }

        // dummy fix as me and pretty much nobody else currently knows HOW EXACTLY was is supposed to work, womp womp
        // seems to work fine though so why not
        if (component.BleedingAmountRaw > 0)
            component.Scaling = 1;

        Dirty(uid, component);
    }

    [SubscribeLocalEvent]
    public void OnBleedRemoverSeverityUpdate(Entity<BleedRemoverComponent> ent, ref WoundSeverityPointChangedEvent args)
    {
        var delta = args.NewSeverity - args.OldSeverity;
        var part = args.Component.HoldingWoundable;
        if (delta < ent.Comp.SeverityThreshold ||
            TerminatingOrDeleted(part) ||
            _body.GetBody(part) is not {} body)
            return;

        var result = _wound.TryHealBleedingWounds(part,
            delta * ent.Comp.BleedingRemovalMultiplier,
            out _);

        if (!result)
            return;

        _audio.PlayPredicted(new SoundPathSpecifier("/Audio/Effects/lightburn.ogg"), body, body);
        _popup.PopupEntity(Loc.GetString("bloodstream-component-wounds-cauterized"),
            body,
            body,
            PopupType.MediumCaution);
    }

    [SubscribeLocalEvent]
    private void OnBodyUpdate(Entity<BodyComponent> ent, ref BloodstreamUpdateEvent args)
    {
        var total = FixedPoint2.Zero;
        foreach (var part in _body.GetOrgans<WoundableComponent>(ent.AsNullable()))
        {
            var totalPartBleeds = FixedPoint2.Zero;
            foreach (var wound in _wound.GetWoundableWounds(part.AsNullable()))
            {
                if (_bleedQuery.TryComp(wound, out var bleeds))
                    totalPartBleeds += bleeds.BleedingAmount;
            }
            total += totalPartBleeds;

            part.Comp.Bleeds = totalPartBleeds;
            // not dirtied because jesus christ that would spam packets
        }

        var blood = Comp<BloodstreamComponent>(ent);
        blood.BleedAmountFromWounds = (float) total;
        blood.BleedAmount = blood.BleedAmountFromWounds + blood.BleedAmountNotFromWounds;
        blood.BleedAmount = Math.Clamp(blood.BleedAmount, 0, blood.MaxBleedAmount);
        DirtyFields(ent.Owner, blood, null, nameof(BloodstreamComponent.BleedAmount), nameof(BloodstreamComponent.BleedAmountFromWounds));

        if (blood.BleedAmount == 0)
        {
            _alerts.ClearAlert(ent.Owner, blood.BleedingAlert);
        }
        else
        {
            var severity = (short) Math.Clamp(Math.Round(blood.BleedAmount, MidpointRounding.ToZero), 0, 10);
            _alerts.ShowAlert(ent.Owner, blood.BleedingAlert, severity);
        }
    }
}
