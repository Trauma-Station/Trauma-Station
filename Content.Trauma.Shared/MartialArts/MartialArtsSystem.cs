using System.Linq;
using Content.Shared._Shitmed.Targeting;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.IdentityManagement;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Weapons.Melee.Events;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.MartialArts;
using Content.Trauma.Shared.Knowledge.Systems;
using Content.Trauma.Shared.MartialArts.Components;
using Content.Trauma.Shared.MartialArts.Events;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.MartialArts;

/// <summary>
/// Handles most of Martial Arts Systems.
/// </summary>
public sealed partial class MartialArtsSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _modifier = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

    public override void Initialize()
    {
        base.Initialize();
        InitializeCanPerformCombo();

        SubscribeLocalEvent<GrabStagesOverrideComponent, CheckGrabOverridesEvent>(CheckGrabStageOverride);

        SubscribeLocalEvent<MartialArtModifiersComponent, GetMeleeAttackRateEvent>(OnGetMeleeAttackRate);
        SubscribeLocalEvent<MartialArtModifiersComponent, RefreshMovementSpeedModifiersEvent>(OnGetMovespeed);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var query = EntityQueryEnumerator<CanPerformComboComponent>();
        while (query.MoveNext(out var ent, out var comp))
        {
            if (comp.CurrentTarget != null && TerminatingOrDeleted(comp.CurrentTarget.Value))
                comp.CurrentTarget = null;

            if (_timing.CurTime < comp.ResetTime || comp.LastAttacks.Count == 0 && comp.Momentum == 0)
                continue;

            comp.LastAttacks.Clear();
            comp.Momentum = 0;
            Dirty(ent, comp);
        }

        var kravBlockedQuery = EntityQueryEnumerator<KravMagaBlockedBreathingComponent>();
        while (kravBlockedQuery.MoveNext(out var ent, out var comp))
        {
            if (_timing.CurTime < comp.BlockedTime)
                continue;
            RemCompDeferred(ent, comp);
        }

        var meleeAttackRateMultiplierQuery = EntityQueryEnumerator<MartialArtModifiersComponent>();
        while (meleeAttackRateMultiplierQuery.MoveNext(out var ent, out var multiplier))
        {
            if (_timing.CurTime < multiplier.NextUpdate)
                continue;

            double? nextUpdate = null;
            var refreshSpeed = false;
            for (var i = multiplier.Data.Count - 1; i >= 0; i--)
            {
                var data = multiplier.Data[i];

                if (_timing.CurTime < data.EndTime)
                {
                    nextUpdate = nextUpdate == null
                        ? data.EndTime.TotalSeconds
                        : Math.Min(nextUpdate.Value, data.EndTime.TotalSeconds);
                    continue;
                }

                if ((data.Type & MartialArtModifierType.MoveSpeed) != 0)
                    refreshSpeed = true;

                multiplier.Data.RemoveAt(i);
            }

            if (refreshSpeed)
                _modifier.RefreshMovementSpeedModifiers(ent);

            if (multiplier.Data.Count == 0)
                RemCompDeferred(ent, multiplier);
            else
            {
                if (nextUpdate != null)
                    multiplier.NextUpdate = TimeSpan.FromSeconds(nextUpdate.Value);
                Dirty(ent, multiplier);
            }
        }

        if (_netManager.IsClient)
            return;
    }

    #region Event Methods

    private void OnGetMovespeed(Entity<MartialArtModifiersComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        var (mult, _) = GetMultiplierModifier(ent, MartialArtModifierType.MoveSpeed, null);
        args.ModifySpeed(mult, mult);
    }

    private DamageModifierSet GetDamageModifierSet(DamageSpecifier specifier, float multiplier, float modifier)
    {
        return new()
        {
            Coefficients = specifier.DamageDict
                .Select(x => KeyValuePair.Create(x.Key, multiplier))
                .ToDictionary(),
            FlatReduction = specifier.DamageDict
                .Select(x => KeyValuePair.Create(x.Key, -modifier)) // Minus mod because it subtracts values from damage
                .ToDictionary(),
        };
    }

    private void OnGetMeleeAttackRate(Entity<MartialArtModifiersComponent> ent, ref GetMeleeAttackRateEvent args)
    {
        var (mult, mod) = GetMultiplierModifier(ent, MartialArtModifierType.AttackRate, args.Weapon != args.User);
        args.Multipliers *= mult;
        args.Rate += mod;
    }

    private (float mult, float mod) GetMultiplierModifier(Entity<MartialArtModifiersComponent> ent,
        MartialArtModifierType type,
        bool? armed)
    {
        var mult = 1f;
        var mod = 0f;
        foreach (var data in ent.Comp.Data.Where(x => (x.Type & type) != 0))
        {
            if (armed is true)
            {
                if ((data.Type & MartialArtModifierType.Armed) == 0
                    && (data.Type & MartialArtModifierType.Unarmed) != 0)
                    continue;
            }
            else if (armed is false)
            {
                if ((data.Type & MartialArtModifierType.Unarmed) == 0
                    && (data.Type & MartialArtModifierType.Armed) != 0)
                    continue;
            }
            mult *= data.Multiplier;
            mod += data.Modifier;
        }

        foreach (var (_, limit) in ent.Comp.MinMaxModifiersMultipliers.Where(x => (x.Key & type) != 0))
        {
            mult = Math.Clamp(mult, limit.X, limit.Y);
            mod = Math.Clamp(mod, limit.Z, limit.W);
        }

        return (mult, mod);
    }

    private void CheckGrabStageOverride(Entity<GrabStagesOverrideComponent> ent, ref CheckGrabOverridesEvent args)
    {
        if (args.Stage == GrabStage.Soft)
            args.Stage = ent.Comp.StartingStage;
    }

    private void ComboPopup(EntityUid user, EntityUid target, string comboName)
    {
        if (!_netManager.IsServer)
            return;
        var userName = Identity.Entity(user, EntityManager);
        var targetName = Identity.Entity(target, EntityManager);
        _popupSystem.PopupEntity(Loc.GetString("martial-arts-action-sender",
            ("name", targetName),
            ("move", comboName)),
            user,
            user);
        _popupSystem.PopupEntity(Loc.GetString("martial-arts-action-receiver",
            ("name", userName),
            ("move", comboName)),
            target,
            target);
    }

    #endregion

    #region Helper Methods

    private bool TryUseMartialArt(Entity<CanPerformComboComponent> ent,
        ComboPrototype proto,
        out EntityUid target,
        out bool downed)
    {
        target = EntityUid.Invalid;
        downed = false;

        if (ent.Comp.CurrentTarget == null)
            return false;

        if (!proto.CanDoWhileProne && _standing.IsDown(ent.Owner))
        {
            _popupSystem.PopupEntity(Loc.GetString("martial-arts-fail-prone"), ent, ent);
            return false;
        }

        downed = _standing.IsDown(ent.Comp.CurrentTarget.Value);
        target = ent.Comp.CurrentTarget.Value;

        return true;
    }

    private void DoDamage(EntityUid ent,
        EntityUid target,
        string damageType,
        float damageAmount,
        out DamageSpecifier damage,
        TargetBodyPart? targetBodyPart = null)
    {
        damage = new DamageSpecifier();
        if (!TryComp<TargetingComponent>(ent, out var targetingComponent))
            return;
        damage.DamageDict.Add(damageType, damageAmount);
        if (TryComp(ent, out MartialArtModifiersComponent? modifiers))
        {
            var (mult, mod) = GetMultiplierModifier((ent, modifiers), MartialArtModifierType.Damage, false);
            var modifierSet = GetDamageModifierSet(damage, mult, mod);
            damage = DamageSpecifier.ApplyModifierSet(damage, modifierSet);
        }
        _damageable.TryChangeDamage(target,
            damage,
            origin: ent,
            targetPart: targetBodyPart ?? targetingComponent.Target);
    }

    #endregion
}
