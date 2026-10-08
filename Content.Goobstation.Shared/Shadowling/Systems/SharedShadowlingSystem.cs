// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Conversion;
using Content.Goobstation.Shared.Flashbang;
using Content.Goobstation.Shared.LightDetection.Components;
using Content.Goobstation.Shared.LightDetection.Systems;
using Content.Goobstation.Shared.Mindcontrol;
using Content.Goobstation.Shared.Shadowling.Components;
using Content.Shared.Actions;
using Content.Shared.Body;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Inventory;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Random.Helpers;
using Content.Shared.StatusEffectNew;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Ranged.Events;
using Content.Trauma.Common.CollectiveMind;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Goobstation.Shared.Shadowling.Systems;

public abstract partial class SharedShadowlingSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private MobStateSystem _mob = default!;
    [Dependency] private SharedLightDetectionDamageSystem _lightDamage = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedEntityStorageSystem _entityStorage = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private StatusEffectsSystem _status = default!;

    public static readonly ProtoId<OrganCategoryPrototype> HeadCategory = "Head";
    public static readonly ProtoId<OrganCategoryPrototype> TorsoCategory = "Torso";
    public static readonly ProtoId<MarkingPrototype> AbominationHorns = "AbominationHorns";
    public static readonly ProtoId<MarkingPrototype> AbominationTorso = "AbominationTorso";

    #region Event Handlers

    [SubscribeLocalEvent]
    private void BeforeGunShot(Entity<ShadowlingComponent> ent, ref SelfBeforeGunShotEvent args)
    {
        // Slings cant shoot guns
        if (args.Gun.Comp.ClumsyProof)
            return;

        if (!SharedRandomExtensions.PredictedProb(_timing, 0.5f, GetNetEntity(ent)))
            return;

        _damageable.ChangeDamage(ent.Owner, ent.Comp.GunShootFailDamage, origin: ent);

        _stun.TryAddParalyzeDuration(ent, ent.Comp.GunShootFailStunTime);

        args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnFlashbanged(Entity<ShadowlingComponent> ent, ref GetFlashbangedEvent args)
    {
        // Shadowling get damaged from flashbangs
        _damageable.ChangeDamage(ent.Owner, ent.Comp.HeatDamage);
    }

    [SubscribeLocalEvent]
    private void OnMobStateChanged(EntityUid uid, ShadowlingComponent component, MobStateChangedEvent args)
    {
        // Remove all Thralls if shadowling is dead
        if (args.NewMobState is not (MobState.Dead or MobState.Invalid)
            || component.CurrentPhase == ShadowlingPhases.Ascension)
            return;

        foreach (var thrall in component.Thralls)
        {
            _popup.PopupEntity(Loc.GetString("shadowling-dead"), thrall, thrall, PopupType.LargeCaution);
            RemCompDeferred<ThrallComponent>(thrall);
        }

        var ev = new ShadowlingDeathEvent();
        RaiseLocalEvent(ev);
    }

    [SubscribeLocalEvent]
    private void OnDamageModify(EntityUid uid, ShadowlingComponent component, DamageModifyEvent args)
    {
        if (args.Origin is not {} origin
            || !HasComp<ProjectileComponent>(origin))
            return;

        foreach (var (key,_) in args.Damage.DamageDict)
        {
            if (key == "Heat")
                args.Damage += component.HeatDamageProjectileModifier;
        }
    }

    public void OnThrallRemoved(Entity<ShadowlingComponent> ent)
    {
        if (!TryComp<LightDetectionDamageComponent>(ent, out var lightDet))
            return;

        _lightDamage.AddResistance((ent.Owner, lightDet), -ent.Comp.LightResistanceModifier);
    }

    public ProtoId<CollectiveMindPrototype> ShadowMind = "Shadowmind";
    [SubscribeLocalEvent]
    private void OnMapInit(EntityUid uid, ShadowlingComponent component, ref MapInitEvent args)
    {
        _actions.AddAction(uid, ref component.ActionHatchEntity, component.ActionHatch);

        EnsureComp<CollectiveMindComponent>(uid).Channels.Add(ShadowMind);
    }

    [SubscribeLocalEvent]
    private void OnHatch(Entity<ShadowlingComponent> ent, ref HatchEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionHatchEntity);

        StartHatchingProgress(ent);
    }

    private void StartHatchingProgress(Entity<ShadowlingComponent> ent)
    {
        var (uid, comp) = ent;
        if (comp.IsHatching)
            return;

        comp.IsHatching = true;
        Dirty(ent);

        // Drop all items
        if (TryComp<InventoryComponent>(uid, out var inv))
        {
            foreach (var slot in inv.Slots)
            {
                _inventory.DropSlotContents((uid, inv), slot.Name);
            }
        }

        var egg = PredictedSpawnAtPosition(comp.Egg, Transform(uid).Coordinates);
        if (TryComp<HatchingEggComponent>(egg, out var eggComp)
            && TryComp<EntityStorageComponent>(egg, out var eggStorage))
        {
            eggComp.ShadowlingInside = uid;
            _entityStorage.Insert(uid, egg, eggStorage);
        }

        // It should be noted that Shadowling shouldn't be able to take damage during this process.
    }

    [SubscribeLocalEvent]
    private void OnBeforeDamageChanged(Entity<ShadowlingComponent> ent, ref BeforeDamageChangedEvent args)
    {
        // Can't take damage during hatching
        if (ent.Comp.IsHatching)
            args.Cancelled = true;
    }

    public void OnPhaseChanged(EntityUid uid, ShadowlingComponent component, ShadowlingPhases phase)
    {
        var defaultAbilities = ProtoMan.Index(component.PostHatchComponents);
        switch (phase)
        {
            case ShadowlingPhases.PostHatch:
            {
                EntityManager.AddComponents(uid, defaultAbilities);
                _actions.RemoveAction(uid, component.ActionHatchEntity);
                break;
            }
            case ShadowlingPhases.Ascension:
            {
                // Remove all previous actions
                EntityManager.RemoveComponents(uid, defaultAbilities);
                EntityManager.RemoveComponents(uid, ProtoMan.Index(component.ObtainableComponents));

                EntityManager.AddComponents(uid, ProtoMan.Index(component.PostAscensionComponents));

                var ev = new ShadowlingAscendEvent(uid);
                RaiseLocalEvent(ev);
                break;
            }
            case ShadowlingPhases.FailedAscension:
            {
                // git gud bro :sob: :pray:
                EntityManager.RemoveComponents(uid, defaultAbilities);
                EntityManager.RemoveComponents(uid, ProtoMan.Index(component.ObtainableComponents));

                // this is such a big L that even the code is losing and all variables are hardcoded.
                _status.TryAddStatusEffect(uid, "ShadowlingAbominationStatusEffect", out _);
                // mfw i have to write my own marking api :face_holding_back_tears:
                _body.AddOrganMarking(uid, TorsoCategory, AbominationTorso);
                _body.AddOrganMarking(uid, HeadCategory, AbominationHorns);

                // take another hardcoded variable
                _damageable.SetDamageModifierSetId(uid, "ShadowlingAbomination");
                break;
            }
        }
    }

    [SubscribeLocalEvent]
    private void OnExamined(EntityUid uid, ShadowlingComponent comp, ExaminedEvent args)
    {
        if (args.Examiner != uid
            || !TryComp<LightDetectionDamageComponent>(uid, out var lightDet))
            return;

        args.PushMarkup(Loc.GetString("shadowling-examine-self", ("damage", lightDet.ResistanceModifier * lightDet.DamageToDeal.GetTotal())));
    }

    #endregion

    public bool CanEnthrall(EntityUid uid, EntityUid target)
    {
        if (HasComp<ShadowlingComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("shadowling-enthrall-shadowling"), uid, uid, PopupType.SmallCaution);
            return false;
        }

        if (HasComp<ThrallComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("shadowling-enthrall-already-thrall"), uid, uid, PopupType.SmallCaution);
            return false;
        }

        if (!TryComp<MindControllableComponent>(target, out var mindControllable) || mindControllable.ControlledBySomeone)
        {
            _popup.PopupEntity(Loc.GetString("shadowling-enthrall-cant-be-controlled"), uid, uid, PopupType.SmallCaution);
            return false;
        }

        if (!TryComp<MindContainerComponent>(target, out var mind) || !mind.HasMind)
        {
            _popup.PopupEntity(Loc.GetString("shadowling-enthrall-no-mind"), uid, uid, PopupType.SmallCaution);
            return false;
        }

        if (!HasComp<HumanoidProfileComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("shadowling-enthrall-non-humanoid"), uid, uid, PopupType.SmallCaution);
            return false;
        }

        if (!CanGlare(target))
        {
            _popup.PopupEntity(Loc.GetString("shadowling-enthrall-cant-be-controlled"), uid, uid, PopupType.SmallCaution);
            return false;
        }

        // Target needs to be alive
        if (!TryComp<MobStateComponent>(target, out var mobState)
            || !_mob.IsCritical(target, mobState) && !_mob.IsCritical(target, mobState))
            return true;

        _popup.PopupEntity(Loc.GetString("shadowling-enthrall-dead"), uid, uid, PopupType.SmallCaution);
        return false;
    }

    public bool CanGlare(EntityUid target)
    {
        var convEv = new BeforeConversionEvent();
        RaiseLocalEvent(target, ref convEv);

        if (convEv.Blocked) // make all the shit below to use the event in the future tm
            return false;

        return HasComp<MobStateComponent>(target)
               && !HasComp<ShadowlingComponent>(target)
               && !HasComp<ThrallComponent>(target);
    }

    public void DoEnthrall(EntityUid uid, EntProtoId components, SimpleDoAfterEvent args)
    {
        if (args.Cancelled
            || args.Handled
            || args.Target == null)
            return;

        var target = args.Target.Value;

        var thrall = EnsureComp<ThrallComponent>(target);
        thrall.Converter = uid;
        var comps = ProtoMan.Index(components);
        EntityManager.AddComponents(target, comps);

        if (TryComp<ShadowlingComponent>(uid, out var sling))
            sling.Thralls.Add(target);

        _audio.PlayPredicted(
            new SoundPathSpecifier("/Audio/Items/Defib/defib_zap.ogg"),
            target,
            uid,
            AudioParams.Default);

        args.Handled = true;
    }
}
