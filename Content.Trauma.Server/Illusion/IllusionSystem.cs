// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Illusion;
using Content.Server.Atmos.Components;
using Content.Server.Body.Components;
using Content.Server.Cloning;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Server.Popups;
using Content.Shared.Body.Components;
using Content.Shared.Cloning;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Coordinates;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Nutrition.AnimalHusbandry;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Temperature.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Spawners;
using Content.Trauma.Shared.Parry;

namespace Content.Trauma.Server.Illusion;

public sealed partial class IllusionSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;

    [Dependency] private CloningSystem _cloning = default!;
    [Dependency] private TransformSystem _xform = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private NPCSystem _npc = default!;
    [Dependency] private NpcFactionSystem _npcFaction = default!;
    [Dependency] private HTNSystem _htn = default!;
    [Dependency] private MobThresholdSystem _threshold = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private EntityQuery<IllusionSpawnerComponent> _spawnerQuery = default!;
    [Dependency] private EntityQuery<IllusionComponent> _illusionQuery = default!;
    [Dependency] private EntityQuery<MobStateComponent> _mobStateQuery = default!;

    private static readonly ProtoId<CloningSettingsPrototype> Settings = "Illusion";
    private static readonly ProtoId<HTNCompoundPrototype> Compound = "IllusionCompound";

    public List<Type> ComponentsToRemove =
    [
        typeof(RespiratorComponent),
        typeof(BarotraumaComponent),
        typeof(SatiationComponent),
        typeof(ReproductiveComponent),
        typeof(ReproductivePartnerComponent),
        typeof(TemperatureComponent),
        typeof(PacifiedComponent),
        typeof(BloodstreamComponent),
    ];

    [SubscribeLocalEvent]
    private void OnItemCopied(Entity<IllusionSpawnerComponent> ent, ref IllusionHeldItemCopiedEvent args)
    {
        if (!_spawnerQuery.TryComp(args.OriginalItem, out var originalComp))
            return;

        ent.Comp.Chance = originalComp.Chance * originalComp.DegradationRateOnClone;
        ent.Comp.HealthMultiplier = originalComp.HealthMultiplier * originalComp.DegradationRateOnClone;
    }

    [SubscribeLocalEvent]
    private void OnDespawn(Entity<IllusionComponent> ent, ref TimedDespawnEvent args)
    {
        DeathPopup(ent);
    }

    [SubscribeLocalEvent]
    private void OnStateChanged(Entity<IllusionComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Alive)
            return;

        DeathPopup(ent);
        QueueDel(ent);
    }

    private void DeathPopup(Entity<IllusionComponent> ent)
    {
        _popup.PopupCoordinates(Loc.GetString(ent.Comp.DeathMessage, ("ent", Identity.Entity(ent, EntityManager))),
            Transform(ent).Coordinates,
            PopupType.SmallCaution);
    }

    [SubscribeLocalEvent]
    private void OnParry(Entity<IllusionOnParryComponent> ent, ref ParriedWithEvent args)
    {
        if (args.Attacker is not { } attacker)
            return;

        if (!_spawnerQuery.TryComp(ent, out var spawner))
            return;

        var list = new List<EntityUid>() { attacker };

        if (!ShouldSpawnIllusion((ent, spawner), args.User, list))
            return;

        SpawnIllusion(args.User, spawner.Lifetime, spawner.HealthMultiplier, list, spawner.Components);
    }

    [SubscribeLocalEvent]
    private void OnHit(Entity<IllusionOnMeleeHitComponent> ent, ref MeleeHitEvent args)
    {
        if (!_spawnerQuery.TryComp(ent, out var spawner))
            return;

        var list = new List<EntityUid>(args.HitEntities);

        if (!args.IsHit || !ShouldSpawnIllusion((ent, spawner), args.User, list))
            return;

        SpawnIllusion(args.User, spawner.Lifetime, spawner.HealthMultiplier, list, spawner.Components);
    }

    private void SpawnIllusion(EntityUid user,
        float lifetime,
        float healthMultiplier,
        IReadOnlyList<EntityUid> targets,
        ComponentRegistry components)
    {
        if (lifetime <= 0f || healthMultiplier <= 0f)
            return;

        var hp = _threshold.GetLowestThreshold(user);

        if (hp <= 0)
            return;

        var damage = _threshold.CheckVitalDamage(user);
        var cloneHp = (hp - damage) * healthMultiplier;
        if (cloneHp <= 0)
            return;

        var xform = Transform(user);
        if (!_cloning.TryCloning(user, _xform.GetMapCoordinates(xform), Settings, out var clone))
            return;

        _threshold.SetMobStateThreshold(clone.Value, cloneHp, MobState.SoftCrit);

        EnsureComp<TimedDespawnComponent>(clone.Value).Lifetime = lifetime;
        EnsureComp<IllusionComponent>(clone.Value);

        foreach (var comp in ComponentsToRemove)
        {
            RemComp(clone.Value, comp);
        }

        if (TryComp(user, out HandsComponent? hands))
        {
            foreach (var held in _hands.EnumerateHeld((user, hands)))
            {
                if (_cloning.CopyItem(held, xform.Coordinates, copyStorage: false) is not { } itemClone)
                    continue;

                if (!_hands.TryPickupAnyHand(clone.Value, itemClone, checkActionBlocker: false, animateUser: false, animate: false))
                {
                    QueueDel(itemClone);
                    continue;
                }

                EnsureComp<UnremoveableComponent>(itemClone);
                var ev = new IllusionHeldItemCopiedEvent(held, itemClone);
                RaiseLocalEvent(itemClone, ref ev);
            }
        }

        var htn = EnsureComp<HTNComponent>(clone.Value);
        htn.RootTask = new HTNCompoundTask { Task = Compound };
        _npc.SetBlackboard(clone.Value, NPCBlackboard.FollowTarget, user.ToCoordinates(), htn);

        var exception = EnsureComp<FactionExceptionComponent>(clone.Value);
        _npcFaction.IgnoreEntity((clone.Value, exception), user);
        _npcFaction.AggroEntities((clone.Value, exception), targets);

        _htn.Replan(htn);

        EntityManager.AddComponents(clone.Value, components);
    }

    private bool ShouldSpawnIllusion(Entity<IllusionSpawnerComponent> ent,
        EntityUid user,
        List<EntityUid> targets)
    {
        if (targets.Count == 0)
            return false;

        if (!_random.Prob(ent.Comp.Chance))
            return false;

        if (ent.Comp.FactionWhitelist.Count > 0)
        {
            if (!_npcFaction.IsMemberOfAny(user, ent.Comp.FactionWhitelist))
                return false;

            targets.RemoveAll(x => _npcFaction.IsMemberOfAny(x, ent.Comp.FactionWhitelist));
        }

        foreach (var target in targets)
        {
            if (target == user)
                continue;

            if (_illusionQuery.HasComp(target))
                continue;

            if (!_mobStateQuery.TryComp(target, out var mobState))
                continue;

            if (mobState.CurrentState is MobState.Alive or MobState.Critical)
                return true;
        }

        return false;
    }
}
