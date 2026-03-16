using Content.Shared._DV.CosmicCult.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._DV.CosmicCult.Abilities;

public abstract class SharedCosmicSiphonSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedCosmicCultSystem _cosmicCult = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly MobThresholdSystem _threshold = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private readonly ProtoId<DamageTypePrototype> DamageType = "Cold";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CosmicCultComponent, EventCosmicSiphon>(OnCosmicSiphon);
        SubscribeLocalEvent<CosmicCultComponent, EventCosmicSiphonDoAfter>(OnCosmicSiphonDoAfter);
    }

    // Doesn't check for DivineIntervention. Yes, this is intentional.
    private void OnCosmicSiphon(Entity<CosmicCultComponent> ent, ref EventCosmicSiphon args)
    {
        if (ent.Comp.EntropyLocked)
        {
            _popup.PopupClient(Loc.GetString("cosmicability-siphon-full"), ent, ent);
            return;
        }
        if (_cosmicCult.EntityIsCultist(args.Target) || _mobState.IsDead(args.Target))
        {
            _popup.PopupClient(Loc.GetString("cosmicability-siphon-fail", ("target", Identity.Entity(args.Target, EntityManager))), ent, ent);
            return;
        }
        if (args.Handled)
            return;

        var doargs = new DoAfterArgs(EntityManager, ent, ent.Comp.CosmicSiphonDelay, new EventCosmicSiphonDoAfter(), ent, args.Target)
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

    protected virtual void OnCosmicSiphonDoAfter(Entity<CosmicCultComponent> ent, ref EventCosmicSiphonDoAfter args)
    {
        if (args.Target is not {} target
            || !_timing.IsFirstTimePredicted
            || args.Cancelled
            || args.Handled)
            return;

        args.Handled = true;
        var entropyQuantity = ent.Comp.CosmicSiphonQuantity;

        if (_mobState.IsCritical(target)) // If target is critical, we get way more entropy and kill the target
        {
            entropyQuantity += _whitelist.IsValid(ent.Comp.HighValueTargetWhitelist, target) ?
            ent.Comp.CosmicSiphonQuantityCritHighValue : ent.Comp.CosmicSiphonQuantityCrit;

            if (!_threshold.TryGetThresholdForState(target, MobState.Dead, out var damage))
                return;
            var curDamage = 0f;
            if (TryComp<DamageableComponent>(target, out var damageComp)) curDamage = (float) _damage.GetDamage((target, damageComp)).GetTotal();
            DamageSpecifier dspec = new();
            dspec.DamageDict.Add(DamageType, damage.Value - curDamage + _random.NextFloat(30f, 60f));
            _damage.TryChangeDamage(target, dspec, true);
        }

        RaiseLocalEvent(target, new CosmicSiphonIndicatorEvent());
        _popup.PopupClient(Loc.GetString("cosmicability-siphon-success", ("target", Identity.Entity(target, EntityManager))), ent, ent);
        _cosmicCult.AddEntropy(ent, entropyQuantity);
    }
}
