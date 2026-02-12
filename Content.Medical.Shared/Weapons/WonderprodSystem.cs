// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Actions;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;
using Content.Shared.Damage.Components;
using Content.Shared.DoAfter;
using Content.Shared.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Medical.Shared.Weapons;

/// <summary>
/// Does logic for wonderprod sleep + cuff modes.
/// </summary>
public sealed class WonderprodSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly ReactiveSystem _reactive = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
    [Dependency] private readonly SharedCuffableSystem _cuffs = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CuffsOnHitComponent, MeleeHitEvent>(OnCuffsOnMeleeHit);
        SubscribeLocalEvent<CuffsOnHitComponent, CuffsOnHitDoAfterEvent>(OnCuffsDoAfter);

        SubscribeLocalEvent<InjectOnHitComponent, MeleeHitEvent>(OnInjectOnMeleeHit);
    }

    private void OnCuffsOnMeleeHit(Entity<CuffsOnHitComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || args.HitEntities.Count == 0)
            return;

        foreach (var target in args.HitEntities)
        {
            if (!TryComp<CuffableComponent>(target, out var cuffable) || cuffable.Container.Count != 0)
                continue;

            var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.Duration, new CuffsOnHitDoAfterEvent(), ent, target)
            {
                BreakOnMove = true,
                BreakOnWeightlessMove = false,
                BreakOnDamage = true,
                NeedHand = true,
                DistanceThreshold = 1f
            };

            if (!_doAfter.TryStartDoAfter(doAfterEventArgs))
                continue;

            // TODO SHITMED: predict properly
            _color.RaiseEffect(Color.FromHex("#601653"), new List<EntityUid>(1) { target }, Filter.Pvs(target, entityManager: EntityManager));
        }
    }

    private void OnCuffsDoAfter(Entity<CuffsOnHitComponent> ent, ref CuffsOnHitDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is not {} target)
            return;

        var user = args.User;
        if (!TryComp<CuffableComponent>(target, out var cuffable) || cuffable.Container.Count != 0)
            return;

        args.Handled = true;

        var handcuffs = PredictedSpawnNextToOrDrop(ent.Comp.HandcuffPrototype, args.User);

        if (!_cuffs.TryAddNewCuffs(target, user, handcuffs, cuffable))
            PredictedDel(handcuffs);
    }

    private void OnInjectOnMeleeHit(Entity<InjectOnHitComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || args.HitEntities.Count == 0)
            return;

        foreach (var target in args.HitEntities)
        {
            if (!_solution.TryGetInjectableSolution(target, out var targetSoln, out var targetSolution))
                continue;

            var solution = new Solution(ent.Comp.Reagents);
            var bad = false;
            foreach (var reagent in ent.Comp.Reagents)
            {
                if (ent.Comp.ReagentLimit is {} limit && _solution.GetTotalPrototypeQuantity(target, reagent.Reagent.ToString()) >= limit)
                {
                    bad = true;
                    break;
                }
            }
            if (bad)
                continue;

            if (!ent.Comp.NeedsRestrain
                || _mobState.IsIncapacitated(target)
                || HasComp<StunnedComponent>(target)
                || HasComp<KnockedDownComponent>(target)
                || TryComp<CuffableComponent>(target, out var cuffable)
                && _cuffs.IsCuffed((target, cuffable)))
            {
                _reactive.DoEntityReaction(target, solution, ReactionMethod.Injection);
                _solution.TryAddSolution(targetSoln.Value, solution);
            }
            else
            {
                // TODO SHITMED: dont use fucking timer
                Timer.Spawn(ent.Comp.InjectionDelay, () =>
                {
                    _reactive.DoEntityReaction(target, solution, ReactionMethod.Injection);
                    _solution.TryAddSolution(targetSoln.Value, solution);
                });
            }
            // TODO SHITMED: predict properly
            _color.RaiseEffect(Color.FromHex("#0000FF"), new List<EntityUid>(1) { target }, Filter.Pvs(target, entityManager: EntityManager));

            if (ent.Comp.Sound is {} sound)
                _audio.PlayPredicted(sound, target, args.User);
        }
    }
}
