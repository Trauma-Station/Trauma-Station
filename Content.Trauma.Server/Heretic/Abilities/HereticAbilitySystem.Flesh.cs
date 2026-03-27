// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Clothing.Components;
using Content.Medical.Common.Body;
using Content.Medical.Shared.Body;
using Content.Server.Ghost.Roles.Components;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Cloning;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction.Components;
using Content.Shared.Metabolism;
using Content.Shared.Mobs.Components;
using Content.Shared.NPC.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Ranged.Components;
using Content.Trauma.Common.Heretic;
using Content.Trauma.Shared.Heretic.Components.Ghoul;
using Content.Trauma.Shared.Heretic.Components.PathSpecific.Flesh;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Server.Heretic.Abilities;

public sealed partial class HereticAbilitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    private static readonly ProtoId<CloningSettingsPrototype> Settings = "FleshMimic";
    private static readonly ProtoId<OrganCategoryPrototype> StomachCategory = "Stomach";
    private static readonly SoundSpecifier MimicSpawnSound = new SoundCollectionSpecifier("gib");

    protected override void SubscribeFlesh()
    {
        base.SubscribeFlesh();

        SubscribeLocalEvent<FleshPassiveComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<FleshPassiveComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<FleshPassiveComponent, ConsumingFoodEvent>(OnConsumingFood);
    }

    private void OnConsumingFood(Entity<FleshPassiveComponent> ent, ref ConsumingFoodEvent args)
    {
        if (args.Volume <= FixedPoint2.Zero)
            return;

        if (!Heretic.TryGetHereticComponent(ent.Owner, out var heretic, out _) || heretic.PathStage <= 0)
            return;

        var multiplier = GetMultiplier((ent.Owner, ent.Comp), heretic, ref args, out var multipliersApplied);
        if (!multipliersApplied)
            return;

        _blood.TryAddToBloodstream(ent.Owner, new Solution(ent.Comp.ReagentId, multiplier));
    }

    private float GetMultiplier(Entity<FleshPassiveComponent> ent,
        Shared.Heretic.Components.HereticComponent heretic,
        ref ConsumingFoodEvent args,
        out bool multipliersApplied)
    {
        var stage = MathF.Pow(heretic.PathStage, 0.3f);
        var multiplier = args.Volume.Float() * stage;
        var oldMult = multiplier;

        if (HasComp<MobStateComponent>(args.Food))
            multiplier *= ent.Comp.MobMultiplier;
        if (HasComp<BrainComponent>(args.Food))
            multiplier *= ent.Comp.BrainMultiplier;
        if (HasComp<InternalOrganComponent>(args.Food))
            multiplier *= ent.Comp.OrganMultiplier;
        else if (HasComp<OrganComponent>(args.Food))
            multiplier *= ent.Comp.BodyPartMultiplier;
        if (HasComp<Shared.Heretic.Components.HumanOrganComponent>(args.Food))
            multiplier *= ent.Comp.HumanMultiplier;
        if (heretic.Ascended)
            multiplier *= ent.Comp.AscensionMultiplier;

        multipliersApplied = oldMult < multiplier;
        return multiplier * ent.Comp.ReagentMultiplier;
    }

    private void OnMapInit(Entity<FleshPassiveComponent> ent, ref MapInitEvent args)
    {
        ResolveStomach(ent);
    }

    private EntityUid? ResolveStomach(Entity<FleshPassiveComponent> ent)
    {
        if (ent.Comp.Stomach is { } stomach)
            return stomach;

        if (_body.GetOrgan(ent.Owner, StomachCategory) is not { } uid)
            return null;

        if (_solution.TryGetSolution(uid, StomachSystem.DefaultSolutionName, out var sol))
            _solution.SetCapacity(sol.Value, 1984); // hungry
        if (TryComp<InternalOrganComponent>(uid, out var organ))
        {
            organ.IntegrityCap = 1984;
            organ.OrganIntegrity = 1984;
            Dirty(uid, organ);
        }

        if (TryComp<MetabolizerComponent>(uid, out var metabolizer))
        {
            metabolizer.UpdateInterval = TimeSpan.FromSeconds(0.1);
            metabolizer.MaxReagentsProcessable = 10;
            Dirty(uid, metabolizer);
        }

        EnsureComp<UnremoveableOrganComponent>(uid); // no gamer stomach for chuddies that try to steal it

        return ent.Comp.Stomach = uid;
    }

    private void OnDamageChanged(Entity<FleshPassiveComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.DamageDelta == null)
            return;

        if (_mobstate.IsDead(ent))
            return;

        var damage = args.DamageDelta.GetTotal();

        if (damage <= 0)
            return;

        if (!Heretic.TryGetHereticComponent(ent.Owner, out var heretic, out var mind) || !heretic.Ascended)
            return;

        ent.Comp.TrackedDamage += damage;

        if (ent.Comp.TrackedDamage < ent.Comp.MimicDamage)
            return;

        ent.Comp.FleshMimics.RemoveAll(x => !Exists(x));

        if (ent.Comp.MaxMimics <= ent.Comp.FleshMimics.Count)
        {
            var toHeal = -ent.Comp.TrackedDamage / ent.Comp.FleshMimics.Count * ent.Comp.MimicHealMultiplier;
            ent.Comp.TrackedDamage = FixedPoint2.Zero;
            foreach (var mimic in ent.Comp.FleshMimics)
            {
                IHateWoundMed(mimic, AllDamage * toHeal, null, null);
            }

            return;
        }

        var maxToSpawn = ent.Comp.MaxMimics - ent.Comp.FleshMimics.Count;
        var toSpawn = (int) (ent.Comp.TrackedDamage / ent.Comp.MimicDamage);
        toSpawn = Math.Clamp(toSpawn, 0, maxToSpawn);

        if (toSpawn == 0)
            return;

        for (var i = 0; i < toSpawn; i++)
        {
            if (CreateFleshMimic(ent, ent, mind, true, true, 50, args.Origin) is { } clone)
                ent.Comp.FleshMimics.Add(clone);
        }

        ent.Comp.TrackedDamage -= toSpawn * ent.Comp.MimicDamage;
    }

    public override EntityUid? CreateFleshMimic(EntityUid uid,
        EntityUid user,
        EntityUid userMind,
        bool giveBlade,
        bool makeGhostRole,
        FixedPoint2 hp,
        EntityUid? hostile)
    {
        if (_mobstate.IsDead(uid) || HasComp<GhoulComponent>(uid) || HasComp<BorgChassisComponent>(uid))
            return null;

        var xform = Transform(uid);
        if (!_cloning.TryCloning(uid, _xform.GetMapCoordinates(xform), Settings, out var clone))
            return null;

        _aud.PlayPvs(MimicSpawnSound, xform.Coordinates);

        EntityUid? weapon = null;
        if (!giveBlade && TryComp(uid, out HandsComponent? hands))
        {
            foreach (var held in _hands.EnumerateHeld((uid, hands)))
            {
                if (HasComp<GunComponent>(held))
                {
                    weapon = held;
                    break;
                }

                if (HasComp<MeleeWeaponComponent>(held) && weapon == null)
                    weapon = held;
            }
        }

        var minion = EnsureComp<HereticMinionComponent>(clone.Value);
        minion.BoundHeretic = user;
        minion.MinionId = GetNetEntity(userMind).Id;
        Dirty(clone.Value, minion);

        var ghoul = Factory.GetComponent<GhoulComponent>();
        ghoul.GiveBlade = giveBlade;
        ghoul.TotalHealth = hp;
        ghoul.DeathBehavior = GhoulDeathBehavior.Gib;
        ghoul.GhostRoleName = "ghostrole-flesh-mimic-name";
        ghoul.GhostRoleDesc = "ghostrole-flesh-mimic-desc";
        if (weapon != null && _cloning.CopyItem(weapon.Value, xform.Coordinates, copyStorage: false) is { } weaponClone)
        {
            if (!_hands.TryPickup(clone.Value, weaponClone, null, false, false, false))
                QueueDel(weaponClone);
            else
            {
                EnsureComp<GhoulWeaponComponent>(weaponClone);
                ghoul.BoundWeapon = weaponClone;
                var cartridgeQuery = GetEntityQuery<CartridgeAmmoComponent>();
                if (TryComp(weaponClone, out ContainerManagerComponent? containerManager))
                {
                    foreach (var container in containerManager.Containers.Values)
                    {
                        foreach (var contained in container.ContainedEntities)
                        {
                            if (!cartridgeQuery.HasComp(contained))
                                EnsureComp<UnremoveableComponent>(contained);
                        }
                    }
                }
            }
        }

        AddComp(clone.Value, ghoul);

        if (TryComp(uid, out KnockedDownComponent? knocked))
        {
            var time = knocked.NextUpdate - Timing.CurTime;
            if (time > TimeSpan.Zero)
                _stun.TryKnockdown(clone.Value, time, drop: false);
        }

        var damage = EnsureComp<DamageOverTimeComponent>(clone.Value);
        damage.Damage = new DamageSpecifier
        {
            DamageDict =
            {
                { "Blunt", 0.3 },
                { "Slash", 0.3 },
                { "Piercing", 0.3 },
            }
        };
        damage.MultiplierIncrease = 0.02f;
        damage.IgnoreResistances = true;
        Dirty(clone.Value, damage);

        if (!makeGhostRole)
            RemCompDeferred<GhostTakeoverAvailableComponent>(clone.Value);
        else if (TryComp(clone.Value, out GhostRoleComponent? ghostRole))
            ghostRole.RaffleConfig = null;

        var exception = EnsureComp<FactionExceptionComponent>(clone.Value);
        _npcFaction.IgnoreEntity((clone.Value, exception), user);
        if (user != uid)
        {
            _npcFaction.AggroEntity((clone.Value, exception), uid);
            EnsureComp<FleshMimickedComponent>(uid).FleshMimics.Add(clone.Value);
        }

        if (hostile != null && hostile.Value != user)
        {
            _npcFaction.AggroEntity((clone.Value, exception), hostile.Value);
            EnsureComp<FleshMimickedComponent>(hostile.Value).FleshMimics.Add(clone.Value);
        }

        return clone.Value;
    }
}
