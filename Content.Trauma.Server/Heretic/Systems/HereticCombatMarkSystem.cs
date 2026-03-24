// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Medical.Common.Targeting;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Medical;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Content.Trauma.Server.Heretic.Abilities;
using Content.Trauma.Server.Heretic.Systems.PathSpecific;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Cosmos;
using Content.Trauma.Shared.Heretic.Systems;
using Content.Trauma.Shared.Heretic.Systems.PathSpecific.Blade;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.Heretic.Systems;

public sealed class HereticCombatMarkSystem : SharedHereticCombatMarkSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly ProtectiveBladeSystem _pbs = default!;
    [Dependency] private readonly VoidCurseSystem _voidcurse = default!;
    [Dependency] private readonly VomitSystem _vomit = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly StarMarkSystem _starMark = default!;
    [Dependency] private readonly HereticAbilitySystem _ability = default!;
    [Dependency] private readonly HereticSystem _heretic = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticCombatMarkComponent, ComponentStartup>(OnStart);
        SubscribeLocalEvent<HereticCombatMarkComponent, ComponentRemove>(OnRemove);

        SubscribeLocalEvent<HereticCosmicMarkComponent, ComponentRemove>(OnCosmicRemove);
    }

    public override bool ApplyMarkEffect(EntityUid target,
        HereticCombatMarkComponent mark,
        string? path,
        EntityUid user,
        Entity<HereticComponent> heretic)
    {
        if (!base.ApplyMarkEffect(target, mark, path, user, heretic))
            return false;

        switch (path)
        {
            case "Ash":
                _stamina.TakeStaminaDamage(target, 6f * mark.Repetitions);

                var dmg = new DamageSpecifier
                {
                    DamageDict =
                    {
                        { "Heat", 3f * mark.Repetitions },
                    },
                };

                _damageable.TryChangeDamage(target, dmg, origin: user, targetPart: TargetBodyPart.All);
                break;

            case "Blade":
                _pbs.AddProtectiveBlade(user);
                break;

            case "Flesh":
                _ability.CreateFleshMimic(target, user, heretic, false, true, 50, null);
                break;

            case "Lock":
                _status.TryUpdateStatusEffectDuration(target, "LockMarkedStatusEffect", TimeSpan.FromSeconds(20));
                break;

            case "Rust":
                _vomit.Vomit(target);
                _stun.KnockdownOrStun(target, TimeSpan.FromSeconds(20));
                break;

            case "Void":
                _voidcurse.DoCurse(target, 3);
                break;

            case "Cosmos":
                if (!TryComp(target, out HereticCosmicMarkComponent? cosmicMark))
                    break;

                var targetCoords = Transform(target).Coordinates;
                _starMark.SpawnCosmicField(targetCoords, heretic.Comp.PathStage, predicted: false);

                if (Exists(cosmicMark.CosmicDiamondUid))
                {
                    Spawn(cosmicMark.CosmicCloud, targetCoords);
                    var newCoords = Transform(cosmicMark.CosmicDiamondUid.Value).Coordinates;
                    _pulling.StopAllPulls(target);
                    _transform.SetCoordinates(target, newCoords);
                    Spawn(cosmicMark.CosmicCloud, newCoords);
                    Del(cosmicMark.CosmicDiamondUid.Value); // Just in case
                }

                _stun.TryUpdateParalyzeDuration(target, cosmicMark.ParalyzeTime);
                break;

            default:
                return false;
        }

        var repetitions = mark.Repetitions - 1;
        if (repetitions <= 0)
            return true;

        // transfers the mark to the next nearby person
        var look = _lookup.GetEntitiesInRange(target, 5f, flags: LookupFlags.Dynamic)
            .Where(x => x != target && HasComp<HumanoidProfileComponent>(x) && !_heretic.IsHereticOrGhoul(x))
            .ToList();
        if (look.Count == 0)
            return true;

        _random.Shuffle(look);
        var lookent = look.First();

        var markComp = EnsureComp<HereticCombatMarkComponent>(lookent);
        markComp.DisappearTime = markComp.MaxDisappearTime;
        markComp.Path = path;
        markComp.Repetitions = repetitions;
        Dirty(lookent, markComp);
        return true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        foreach (var comp in EntityQuery<HereticCombatMarkComponent>())
        {
            if (_timing.CurTime > comp.Timer)
                RemComp(comp.Owner, comp);
        }
    }

    private void OnStart(Entity<HereticCombatMarkComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.Timer == TimeSpan.Zero)
            ent.Comp.Timer = _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.DisappearTime);
    }

    private void OnRemove(Entity<HereticCombatMarkComponent> ent, ref ComponentRemove args)
    {
        if (TerminatingOrDeleted(ent.Owner))
            return;

        RemComp<HereticCosmicMarkComponent>(ent.Owner);
    }

    private void OnCosmicRemove(Entity<HereticCosmicMarkComponent> ent, ref ComponentRemove args)
    {
        if (TerminatingOrDeleted(ent.Comp.CosmicDiamondUid))
            return;

        Del(ent.Comp.CosmicDiamondUid);
    }
}
