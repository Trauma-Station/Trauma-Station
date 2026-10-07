// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.CosmicCult.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.CosmicCult.Abilities;

public sealed partial class CosmicSiphonSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedCosmicCultSystem _cosmicCult = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private MobThresholdSystem _threshold = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private IRobustRandom _random = default!;

    private readonly ProtoId<DamageTypePrototype> _damageType = "Cold";

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

        args.Handled = true;
        var entropyQuantity = ent.Comp.CosmicSiphonQuantity;

        if (_mobState.IsCritical(args.Target)) // If target is critical, we get way more entropy and kill the target
        {
            entropyQuantity += _whitelist.IsValid(ent.Comp.HighValueTargetWhitelist, args.Target)
                ? ent.Comp.CosmicSiphonQuantityCritHighValue
                : ent.Comp.CosmicSiphonQuantityCrit;

            if (!_threshold.TryGetThresholdForState(args.Target, MobState.Dead, out var damage))
                return;

            var curDamage = _damage.GetTotalDamage(args.Target).Float();
            DamageSpecifier dspec = new();
            dspec.DamageDict.Add(_damageType, damage.Value - curDamage + _random.NextFloat(30f, 60f));
            _damage.TryChangeDamage(args.Target, dspec, true);
        }

        if (_timing.IsFirstTimePredicted)
            RaiseLocalEvent(args.Target, new CosmicSiphonIndicatorEvent());
        _popup.PopupEntity(Loc.GetString("cosmicability-siphon-success", ("target", Identity.Entity(args.Target, EntityManager))), ent, ent);
        _cosmicCult.AddEntropy(ent, entropyQuantity);
    }
}
