// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Common.Body;
using Content.Medical.Common.CCVar;
using Content.Medical.Shared.Consciousness;
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
using Robust.Shared.Utility;

namespace Content.Medical.Shared.Body;

public sealed partial class BodyBloodstreamSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly ConsciousnessSystem _consciousness = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly WoundSystem _wound = default!;

    private float _bleedingSeverity = 1f;

    private void InitializeWounds()
    {
        base.Initialize();

        SubscribeLocalEvent<BleedInflicterComponent, WoundSeverityPointChangedEvent>(OnBleedInflicterSeverityUpdate);
        SubscribeLocalEvent<BleedRemoverComponent, WoundSeverityPointChangedEvent>(OnBleedRemoverSeverityUpdate);
        SubscribeLocalEvent<BleedInflicterComponent, WoundHealAttemptEvent>(OnWoundHealAttempt);
        SubscribeLocalEvent<BleedInflicterComponent, WoundAddedEvent>(OnWoundAdded);

        SubscribeLocalEvent<BodyComponent, BloodstreamUpdateEvent>(OnBodyUpdate);
        SubscribeLocalEvent<ConsciousnessComponent, BloodstreamUpdateEvent>(OnConsciousnessUpdate);

        Subs.CVar(_cfg, SurgeryCVars.BleedingSeverityTrade, x => _bleedingSeverity = x, true);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var bleedsQuery = EntityQueryEnumerator<BleedInflicterComponent>();
        while (bleedsQuery.MoveNext(out var ent, out var bleeds))
        {
            var canBleed = CanWoundBleed(ent, bleeds) && bleeds.BleedingAmount > 0;
            if (canBleed != bleeds.IsBleeding)
                Dirty(ent, bleeds);

            bleeds.IsBleeding = canBleed;

            if (!bleeds.IsBleeding)
                continue;

            var totalTime = bleeds.ScalingFinishesAt - bleeds.ScalingStartsAt;
            var currentTime = bleeds.ScalingFinishesAt - _timing.CurTime;

            if (totalTime <= currentTime || bleeds.ScalingLimit >= bleeds.Scaling)
                continue;

            var newBleeds = FixedPoint2.Clamp(
                (totalTime / currentTime) / (bleeds.ScalingLimit - bleeds.Scaling),
                0,
                bleeds.ScalingLimit);

            bleeds.Scaling = newBleeds;
            Dirty(ent, bleeds);
        }
    }

    /// <summary>
    /// Add a bleed-ability modifier on woundable
    /// </summary>
    /// <param name="woundable">Entity uid of the woundable to apply the modifiers</param>
    /// <param name="identifier">string identifier of the modifier</param>
    /// <param name="priority">Priority of the said modifier</param>
    /// <param name="canBleed">Should the wounds bleed?</param>
    /// <param name="force">If forced, won't stop after failing to apply one modifier</param>
    /// <param name="woundableComp">Woundable Component</param>
    /// <returns>Return true if applied</returns>
    public bool TryAddBleedModifier(
        EntityUid woundable,
        string identifier,
        int priority,
        bool canBleed,
        bool force = false,
        WoundableComponent? woundableComp = null)
    {
        if (!Resolve(woundable, ref woundableComp))
            return false;

        foreach (var woundEnt in _wound.GetWoundableWounds(woundable, woundableComp))
        {
            if (!TryComp<BleedInflicterComponent>(woundEnt, out var bleedsComp))
                continue;

            if (TryAddBleedModifier(woundEnt, identifier, priority, canBleed, bleedsComp))
                continue;

            if (!force)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Add a bleed-ability modifier
    /// </summary>
    /// <param name="uid">Entity uid of the wound</param>
    /// <param name="identifier">string identifier of the modifier</param>
    /// <param name="priority">Priority of the said modifier</param>
    /// <param name="canBleed">Should the wound bleed?</param>
    /// <param name="comp">Bleed Inflicter Component</param>
    /// <returns>Return true if applied</returns>
    public bool TryAddBleedModifier(
        EntityUid uid,
        string identifier,
        int priority,
        bool canBleed,
        BleedInflicterComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return false;

        if (!comp.BleedingModifiers.TryAdd(identifier, (priority, canBleed)))
            return false;

        Dirty(uid, comp);
        return true;
    }

    /// <summary>
    /// Remove a bleed-ability modifier from a woundable
    /// </summary>
    /// <param name="uid">Entity uid of the woundable</param>
    /// <param name="identifier">string identifier of the modifier</param>
    /// <param name="force">If forced, won't stop applying modifiers after failing one wound</param>
    /// <param name="woundable">Woundable Component</param>
    /// <returns>Returns true if removed all modifiers ON WOUNDABLE</returns>
    public bool TryRemoveBleedModifier(
        EntityUid uid,
        string identifier,
        bool force = false,
        WoundableComponent? woundable = null)
    {
        if (!Resolve(uid, ref woundable))
            return false;

        foreach (var woundEnt in _wound.GetWoundableWounds(uid, woundable))
        {
            if (!TryComp<BleedInflicterComponent>(woundEnt, out var bleedsComp))
                continue;

            if (TryRemoveBleedModifier(woundEnt, identifier, bleedsComp))
                continue;

            if (!force)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Remove a bleed-ability modifier
    /// </summary>
    /// <param name="uid">Entity uid of the wound</param>
    /// <param name="identifier">string identifier of the modifier</param>
    /// <param name="comp">Bleed Inflicter Component</param>
    /// <returns>Return true if removed</returns>
    public bool TryRemoveBleedModifier(
        EntityUid uid,
        string identifier,
        BleedInflicterComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return false;

        if (!comp.BleedingModifiers.Remove(identifier))
            return false;

        Dirty(uid, comp);
        return true;
    }

    /// <summary>
    /// Redact a modifiers meta data
    /// </summary>
    /// <param name="wound">The wound entity uid</param>
    /// <param name="identifier">Identifier of the modifier</param>
    /// <param name="priority">Priority to set</param>
    /// <param name="canBleed">Should it bleed?</param>
    /// <param name="bleeds">Bleed Inflicter Component</param>
    /// <returns>true if was changed</returns>
    public bool ChangeBleedsModifierMetadata(
        EntityUid wound,
        string identifier,
        int priority,
        bool? canBleed,
        BleedInflicterComponent? bleeds = null)
    {
        if (!Resolve(wound, ref bleeds))
            return false;

        if (!bleeds.BleedingModifiers.TryGetValue(identifier, out var pair))
            return false;

        bleeds.BleedingModifiers[identifier] = (Priority: priority, CanBleed: canBleed ?? pair.CanBleed);
        return true;
    }

    /// <summary>
    /// Redact a modifiers meta data
    /// </summary>
    /// <param name="wound">The wound entity uid</param>
    /// <param name="identifier">Identifier of the modifier</param>
    /// <param name="priority">Priority to set</param>
    /// <param name="canBleed">Should it bleed?</param>
    /// <param name="bleeds">Bleed Inflicter Component</param>
    /// <returns>true if was changed</returns>
    public bool ChangeBleedsModifierMetadata(
        EntityUid wound,
        string identifier,
        bool canBleed,
        int? priority,
        BleedInflicterComponent? bleeds = null)
    {
        if (!Resolve(wound, ref bleeds))
            return false;

        if (!bleeds.BleedingModifiers.TryGetValue(identifier, out var pair))
            return false;

        bleeds.BleedingModifiers[identifier] = (Priority: priority ?? pair.Priority, CanBleed: canBleed);
        return true;
    }

    /// <summary>
    /// Self-explanatory
    /// </summary>
    /// <param name="uid">Wound entity</param>
    /// <param name="comp">Bleeds Inflicter Component </param>
    /// <returns>Returns whether if the wound can bleed</returns>
    public bool CanWoundBleed(EntityUid uid, BleedInflicterComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return false;

        var nearestModifier = comp.BleedingModifiers.FirstOrNull();
        if (nearestModifier == null)
            return true; // No modifiers. return true

        var lastCanBleed = true;
        var lastPriority = 0;
        foreach (var (_, pair) in comp.BleedingModifiers)
        {
            if (pair.Priority <= lastPriority)
                continue;

            lastPriority = pair.Priority;
            lastCanBleed = pair.CanBleed;
        }

        return lastCanBleed;
    }

    private void OnWoundAdded(EntityUid uid, BleedInflicterComponent component, ref WoundAddedEvent args)
    {
        if (!CanWoundBleed(uid, component)
            || args.Component.WoundSeverityPoint < component.SeverityThreshold
            || !args.Woundable.CanBleed)
            return;

        // wounds that BLEED will not HEAL.
        component.BleedingAmountRaw = args.Component.WoundSeverityPoint * _bleedingSeverity;

        var formula = (float) (args.Component.WoundSeverityPoint / _cfg.GetCVar(SurgeryCVars.BleedsScalingTime) * component.ScalingSpeed);
        component.ScalingFinishesAt = _timing.CurTime + TimeSpan.FromSeconds(formula);
        component.ScalingStartsAt = _timing.CurTime;
        component.IsBleeding = true;

        Dirty(uid, component);

        _bloodstream.TryModifyBleedAmount(uid, component.BleedingAmountRaw.Float());
    }

    private void OnWoundHealAttempt(EntityUid uid, BleedInflicterComponent component, ref WoundHealAttemptEvent args)
    {
        if (args.IgnoreBlockers)
            return;

        if (component.IsBleeding)
            args.Cancelled = true;
    }

    private void OnBleedInflicterSeverityUpdate(EntityUid uid,
        BleedInflicterComponent component,
        ref WoundSeverityPointChangedEvent args)
    {
        if (!CanWoundBleed(uid, component)
            || !TryComp<WoundableComponent>(args.Component.HoldingWoundable, out var woundable)
            || !woundable.CanBleed
            || args.NewSeverity < component.SeverityThreshold
            || args.NewSeverity < args.OldSeverity)
            return;

        var oldBleedsAmount = args.OldSeverity * _cfg.GetCVar(SurgeryCVars.BleedingSeverityTrade);
        component.BleedingAmountRaw = args.NewSeverity * _cfg.GetCVar(SurgeryCVars.BleedingSeverityTrade);

        var severityPenalty = component.BleedingAmountRaw - oldBleedsAmount / _cfg.GetCVar(SurgeryCVars.BleedsScalingTime);
        component.SeverityPenalty += severityPenalty;

        var formula = (float) (args.NewSeverity / _cfg.GetCVar(SurgeryCVars.BleedsScalingTime) * component.ScalingSpeed);
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
        if (component.BleedingAmountRaw > 0) // Goobstation
        {
            component.Scaling = 1;
        }

        Dirty(uid, component);
    }

    public void OnBleedRemoverSeverityUpdate(EntityUid uid, BleedRemoverComponent component, ref WoundSeverityPointChangedEvent args)
    {
        var delta = args.NewSeverity - args.OldSeverity;
        if (delta < component.SeverityThreshold
            || !TryComp(uid, out WoundComponent? wound)
            || TerminatingOrDeleted(wound.HoldingWoundable)
            || !TryComp(wound.HoldingWoundable, out WoundableComponent? woundable)
            || _body.GetBody(wound.HoldingWoundable) is not {} body)
            return;

        var result = _wound.TryHealBleedingWounds(wound.HoldingWoundable,
            (-delta * component.BleedingRemovalMultiplier).Float(),
            out _,
            woundable);

        if (!result)
            return;

        // TODO SHITMED: predicted wounds popups etc
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/lightburn.ogg"), body);
        _popup.PopupEntity(Loc.GetString("bloodstream-component-wounds-cauterized"),
            body,
            body,
            PopupType.MediumCaution);
    }

    private void OnBodyUpdate(Entity<BodyComponent> ent, ref BloodstreamUpdateEvent args)
    {
        var total = FixedPoint2.Zero;
        foreach (var part in _body.GetOrgans<WoundableComponent>(ent.AsNullable()))
        {
            var totalPartBleeds = FixedPoint2.Zero;
            foreach (var (wound, _) in _wound.GetWoundableWounds(part, part.Comp))
            {
                if (TryComp<BleedInflicterComponent>(wound, out var bleeds))
                    totalPartBleeds += bleeds.BleedingAmount;
            }
            total += totalPartBleeds;

            part.Comp.Bleeds = totalPartBleeds;
            // not dirtied because jesus christ that would spam packets
        }

        var blood = Comp<BloodstreamComponent>(ent);
        blood.BleedAmount = blood.BleedAmountFromWounds + blood.BleedAmountNotFromWounds;
        blood.BleedAmount = Math.Clamp(blood.BleedAmount, 0, blood.MaxBleedAmount);
        blood.BleedAmountFromWounds = (float) total;
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

    // TODO SHITMED: this should be removed if consciousness is tied to blood oxygenation
    private void OnConsciousnessUpdate(Entity<ConsciousnessComponent> ent, ref BloodstreamUpdateEvent args)
    {
        if (!_consciousness.TryGetNerveSystem(ent, out var brain))
            return;

        var level = _bloodstream.GetBloodLevel(ent.Owner);
        if (level >= 1f)
        {
            _consciousness.RemoveConsciousnessModifier(ent, brain.Value, "Bleeding", ent.Comp);
            return;
        }

        // 0% blood -> -75 consciouness modifier
        var multiplier = FixedPoint2.New(75) * (1f - level);
        _consciousness.SetConsciousnessModifier(
            ent.Owner,
            brain.Value,
            multiplier,
            "Bleeding",
            ConsciousnessModType.Pain, // TODO SHITMED: no it fucking isnt, your brain is losing access to oxygen. that's not pain katsap
            null,
            ent.Comp);
    }
}
