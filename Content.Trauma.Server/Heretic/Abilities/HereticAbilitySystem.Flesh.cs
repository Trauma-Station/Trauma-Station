// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Clothing.Components;
using Content.Goobstation.Shared.Disease.Components;
using Content.Medical.Common.Body;
using Content.Medical.Shared.Body;
using Content.Server.Ghost.Roles.Components;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Cloning;
using Content.Shared.Damage;
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

namespace Content.Trauma.Server.Heretic.Abilities;

public sealed partial class HereticAbilitySystem
{
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    [Dependency] private EntityQuery<CartridgeAmmoComponent> _cartridgeQuery = default!;

    private static readonly ProtoId<CloningSettingsPrototype> Settings = "FleshMimic";
    private static readonly ProtoId<OrganCategoryPrototype> StomachCategory = "Stomach";
    private static readonly SoundSpecifier MimicSpawnSound = new SoundCollectionSpecifier("gib");

    protected override void SubscribeFlesh()
    {
        base.SubscribeFlesh();

        SubscribeLocalEvent<FleshPassiveComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<FleshPassiveComponent, ConsumingFoodEvent>(OnConsumingFood);
    }

    private void OnConsumingFood(Entity<FleshPassiveComponent> ent, ref ConsumingFoodEvent args)
    {
        if (HasComp<LordOfTheNightComponent>(ent))
            return;

        if (args.Volume <= FixedPoint2.Zero)
            return;

        if (!Heretic.TryGetHereticComponent(ent.Owner, out var heretic, out _))
            return;

        var multiplier = GetMultiplier((ent.Owner, ent.Comp), heretic, ref args, out var multipliersApplied);
        if (!multipliersApplied)
            return;

        IHateWoundMed(ent.Owner,
            AllDamage * multiplier * ent.Comp.Heal,
            multiplier * ent.Comp.BloodHeal,
            multiplier * ent.Comp.BleedHeal,
            0);
    }

    private float GetMultiplier(Entity<FleshPassiveComponent> ent,
        Shared.Heretic.Components.HereticComponent heretic,
        ref ConsumingFoodEvent args,
        out bool multipliersApplied)
    {
        var multiplier = args.Volume.Float();
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

        multipliersApplied = oldMult < multiplier;

        if (heretic.Ascended)
            multiplier *= ent.Comp.AscensionMultiplier;

        return multiplier;
    }

    private void OnMapInit(Entity<FleshPassiveComponent> ent, ref MapInitEvent args)
    {
        RemCompDeferred<DiseaseCarrierComponent>(ent);
        ResolveStomach(ent);
    }

    private EntityUid? ResolveStomach(Entity<FleshPassiveComponent> ent)
    {
        if (HasComp<LordOfTheNightComponent>(ent))
            return null;

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
            metabolizer.MetabolizerTypes = [ent.Comp.FleshMetabolizer];
            Dirty(uid, metabolizer);
        }

        EnsureComp<UnremoveableOrganComponent>(uid); // no gamer stomach for chuddies that try to steal it

        return ent.Comp.Stomach = uid;
    }

    public override EntityUid? CreateFleshMimic(EntityUid uid,
        EntityUid user,
        int minionId,
        bool giveBlade,
        bool makeGhostRole,
        FixedPoint2 hp,
        EntityUid? hostile,
        bool hostileToSource)
    {
        if (_mobstate.IsDead(uid) || HasComp<BorgChassisComponent>(uid))
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
        minion.MinionId = minionId;
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
                if (TryComp(weaponClone, out ContainerManagerComponent? containerManager))
                {
                    foreach (var container in containerManager.Containers.Values)
                    {
                        foreach (var contained in container.ContainedEntities)
                        {
                            if (!_cartridgeQuery.HasComp(contained))
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
        if (hostileToSource &&
            (!TryComp(uid, out HereticMinionComponent? minionComp) || minionComp.MinionId != minionId))
        {
            _npcFaction.AggroEntity((clone.Value, exception), uid);
            EnsureComp<FleshMimickedComponent>(uid).FleshMimics.Add(clone.Value);
        }

        if (hostile != null && hostile.Value != user && hostile.Value != uid &&
            (!TryComp(hostile.Value, out minionComp) || minionComp.MinionId != minionId))
        {
            _npcFaction.AggroEntity((clone.Value, exception), hostile.Value);
            EnsureComp<FleshMimickedComponent>(hostile.Value).FleshMimics.Add(clone.Value);
        }

        return clone.Value;
    }
}
