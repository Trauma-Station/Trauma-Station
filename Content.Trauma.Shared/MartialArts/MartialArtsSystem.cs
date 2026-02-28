using System.Linq;
using Content.Medical.Common.Targeting;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.IdentityManagement;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Weapons.Melee.Events;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.MartialArts;
using Content.Trauma.Shared.MartialArts.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.MartialArts;

/// <summary>
/// Handles most of Martial Arts Systems.
/// </summary>
public sealed partial class MartialArtsSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _modifier = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

    public override void Initialize()
    {
        base.Initialize();
        InitializeCanPerformCombo();

        SubscribeLocalEvent<GrabStagesOverrideComponent, CheckGrabOverridesEvent>(CheckGrabStageOverride);

        SubscribeLocalEvent<MartialArtModifiersComponent, RefreshMovementSpeedModifiersEvent>(OnGetMovespeed);
        SubscribeLocalEvent<SneakAttackComponent, InvokeSneakAttackSurprisedEvent>(SneakAttackSurprise);
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
        var curTime = _timing.CurTime;
        while (kravBlockedQuery.MoveNext(out var ent, out var comp))
        {
            if (curTime < comp.BlockedTime)
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

        var sneakAttackQuery = EntityQueryEnumerator<SneakAttackComponent>();
        while (sneakAttackQuery.MoveNext(out var ent, out var sneakAttack))
        {
            if (sneakAttack is { } && sneakAttack.IsFound)
            {
                if (_timing.CurTime >= sneakAttack.FramesTillHidden)
                    sneakAttack.IsFound = false;
            }
        }

        var fastDamageQuery = EntityQueryEnumerator<SneakAttackComponent>();
        while (fastDamageQuery.MoveNext(out var ent, out var sneakAttack))
        {
            if (sneakAttack is { } && sneakAttack.IsFound)
            {
                if (_timing.CurTime >= sneakAttack.FramesTillHidden)
                    sneakAttack.IsFound = false;
            }
        }
    }

    private void SneakAttackSurprise(Entity<SneakAttackComponent> ent, ref InvokeSneakAttackSurprisedEvent args)
    {
        ent.Comp.FramesTillHidden = _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.SecondsTillHidden);
        ent.Comp.IsFound = true;
        Dirty(ent);
    }

    #region Event Methods

    private void OnGetMovespeed(Entity<MartialArtModifiersComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        var (mult, _) = GetMultiplierModifier(ent, MartialArtModifierType.MoveSpeed, null);
        args.ModifySpeed(mult, mult);
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


    #endregion
}
