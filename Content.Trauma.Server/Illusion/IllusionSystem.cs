// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
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
using Content.Shared.Cloning.Events;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Coordinates;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
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
using Content.Trauma.Shared.Parry;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Spawners;

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
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private InventorySystem _inventory = default!;
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
    private void OnItemCopied(Entity<IllusionSpawnerComponent> ent, ref CloningItemEvent args)
    {
        if (!_spawnerQuery.TryComp(args.CloneUid, out var cloneComp))
            return;

        cloneComp.Chance = ent.Comp.Chance * ent.Comp.DegradationRateOnClone;
        cloneComp.HealthMultiplier = ent.Comp.HealthMultiplier * ent.Comp.DegradationRateOnClone;
    }

    [SubscribeLocalEvent]
    private void OnDespawn(Entity<IllusionComponent> ent, ref TimedDespawnEvent args)
    {
        DeathEffects(ent);
    }

    [SubscribeLocalEvent]
    private void OnStateChanged(Entity<IllusionComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Alive)
            return;

        DeathEffects(ent);
        QueueDel(ent);
    }

    private void DeathEffects(Entity<IllusionComponent> ent)
    {
        // In case it picked up some items
        foreach (var item in _inventory.GetHandOrInventoryEntities(ent.Owner))
        {
            _xform.DropNextTo(item, ent.Owner);
        }

        _popup.PopupCoordinates(Loc.GetString(ent.Comp.DeathMessage, ("ent", Identity.Entity(ent, EntityManager))),
            Transform(ent).Coordinates,
            PopupType.SmallCaution);
    }

    [SubscribeLocalEvent]
    private void OnParry(Entity<IllusionOnParryComponent> ent, ref ParriedWithEvent args)
    {
        TrySpawnIllusion(ent.Owner, args.User, args.Attacker is not { } attacker ? null : new List<EntityUid>() { attacker });
    }

    [SubscribeLocalEvent]
    private void OnHit(Entity<IllusionOnMeleeHitComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        TrySpawnIllusion(ent.Owner, args.User, args.HitEntities.Count > 0 ? new(args.HitEntities) : null);
    }

    public bool TrySpawnIllusion(Entity<IllusionSpawnerComponent?> ent, EntityUid user, List<EntityUid>? targets)
    {
        if (!_spawnerQuery.Resolve(ent, ref ent.Comp, false))
            return false;

        if (!ShouldSpawnIllusion(ent!, user, targets))
            return false;

        return SpawnIllusion(user, ent.Comp.Lifetime, ent.Comp.HealthMultiplier, targets, ent.Comp.Components);
    }

    private bool SpawnIllusion(EntityUid user,
        float lifetime,
        float healthMultiplier,
        IReadOnlyList<EntityUid>? targets,
        ComponentRegistry components)
    {
        if (lifetime <= 0f || healthMultiplier <= 0f)
            return false;

        var hp = _threshold.GetLowestThreshold(user);

        if (hp <= 0)
            return false;

        var damage = _threshold.CheckVitalDamage(user);
        var cloneHp = (hp - damage) * healthMultiplier;
        if (cloneHp <= 0)
            return false;

        var xform = Transform(user);
        if (!_cloning.TryCloning(user, _xform.GetMapCoordinates(xform), Settings, out var clone))
            return false;

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
            }
        }

        EntityManager.AddComponents(clone.Value, components);

        var htn = EnsureComp<HTNComponent>(clone.Value);
        htn.RootTask = new HTNCompoundTask { Task = Compound };

        var exception = EnsureComp<FactionExceptionComponent>(clone.Value);
        if (targets?.Contains(user) is not true)
        {
            _npc.SetBlackboard(clone.Value, NPCBlackboard.FollowTarget, user.ToCoordinates(), htn);
            _npcFaction.IgnoreEntity((clone.Value, exception), user);
        }
        if (targets is { } list)
            _npcFaction.AggroEntities((clone.Value, exception), list);

        _htn.Replan(htn);
        return true;
    }

    private bool ShouldSpawnIllusion(Entity<IllusionSpawnerComponent> ent,
        EntityUid user,
        List<EntityUid>? targets)
    {
        if (!_random.Prob(ent.Comp.Chance))
            return false;

        if (!ent.Comp.CanTargetUser)
            targets?.Remove(user);

        if (ent.Comp.FactionWhitelist.Count > 0)
        {
            if (!_npcFaction.IsMemberOfAny(user, ent.Comp.FactionWhitelist))
                return false;

            if (ent.Comp.IgnoreSameFactionTargets)
                targets?.RemoveAll(x => _npcFaction.IsMemberOfAny(x, ent.Comp.FactionWhitelist));
        }

        if (ent.Comp.IgnoreTargets)
            return true;

        if (targets is not { } list)
            return false;

        foreach (var target in list)
        {
            if (_illusionQuery.HasComp(target))
                continue;

            if (!_mobStateQuery.TryComp(target, out var mobState))
                continue;

            if (!_mobState.IsDead(target, mobState))
                return true;
        }

        return false;
    }
}
