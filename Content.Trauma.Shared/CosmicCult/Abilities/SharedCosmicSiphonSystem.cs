// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.CosmicCult.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.CosmicCult.Abilities;

public abstract partial class SharedCosmicSiphonSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedCosmicCultSystem _cosmicCult = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private MobThresholdSystem _threshold = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private IRobustRandom _random = default!;

    private readonly ProtoId<DamageTypePrototype> DamageType = "Cold";

    // Doesn't check for DivineIntervention. Yes, this is intentional.
    [SubscribeLocalEvent]
    private void OnSiphonAction(Entity<CosmicCultComponent> ent, ref CosmicSiphonEvent args)
    {
        if (ent.Comp.EntropyLocked)
        {
            _popup.PopupEntity(Loc.GetString("cosmicability-siphon-full"), ent, ent);
            return;
        }
        if (_cosmicCult.EntityIsCultist(args.Target) || _mobState.IsDead(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("cosmicability-siphon-fail", ("target", Identity.Entity(args.Target, EntityManager))), ent, ent);
            return;
        }
        if (args.Handled)
            return;

        var doargs = new DoAfterArgs(EntityManager, ent, ent.Comp.CosmicSiphonDelay, new CosmicSiphonDoAfterEvent(), ent, args.Target)
        {
            DistanceThreshold = 2.5f,
            Hidden = _mobState.IsAlive(args.Target), // Visible on crit targets
            BreakOnHandChange = false,
            BreakOnDamage = false,
            BreakOnMove = false,
            BreakOnDropItem = false,
        };
        args.Handled = true;
        _doAfter.TryStartDoAfter(doargs);
    }

    [SubscribeLocalEvent]
    protected virtual void OnSiphonDoAfter(Entity<CosmicCultComponent> ent, ref CosmicSiphonDoAfterEvent args)
    {
        if (args.Target is not {} target
            || args.Cancelled
            || args.Handled)
            return;

        args.Handled = true;
        var entropyQuantity = ent.Comp.CosmicSiphonQuantity;

        if (_mobState.IsCritical(target)) // If target is critical, we get way more entropy and kill the target
        {
            entropyQuantity += _whitelist.IsValid(ent.Comp.HighValueTargetWhitelist, target)
                ? ent.Comp.CosmicSiphonQuantityCritHighValue
                : ent.Comp.CosmicSiphonQuantityCrit;

            if (!_threshold.TryGetThresholdForState(target, MobState.Dead, out var damage))
                return;

            var curDamage = _damage.GetTotalDamage(target).Float();
            DamageSpecifier dspec = new();
            dspec.DamageDict.Add(DamageType, damage.Value - curDamage + _random.NextFloat(30f, 60f));
            _damage.TryChangeDamage(target, dspec, true);
        }

        if (_timing.IsFirstTimePredicted)
            RaiseLocalEvent(target, new CosmicSiphonIndicatorEvent());
        _popup.PopupEntity(Loc.GetString("cosmicability-siphon-success", ("target", Identity.Entity(target, EntityManager))), ent, ent);
        _cosmicCult.AddEntropy(ent, entropyQuantity);
    }
}
