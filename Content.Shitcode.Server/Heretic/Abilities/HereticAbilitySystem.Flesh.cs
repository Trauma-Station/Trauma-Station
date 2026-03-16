// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.MartialArts;
using Content.Goobstation.Shared.Clothing.Components;
using Content.Medical.Common.Body;
using Content.Shared.FixedPoint;
using Content.Server.Ghost.Roles.Components;
using Content.Shared._Shitcode.Heretic.Components;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Cloning;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Heretic;
using Content.Shared.Interaction.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.NPC.Components;
using Content.Shared.Stunnable;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.Heretic.Abilities;

public sealed partial class HereticAbilitySystem
{
    private static readonly EntProtoId FleshStomach = "OrganFleshHereticStomach";
    private static readonly ProtoId<CloningSettingsPrototype> Settings = "FleshMimic";
    private static readonly SoundSpecifier MimicSpawnSound = new SoundCollectionSpecifier("gib");

    protected override void SubscribeFlesh()
    {
        base.SubscribeFlesh();

        SubscribeLocalEvent<FleshPassiveComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<FleshPassiveComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<FleshPassiveComponent, ConsumingFoodEvent>(OnConsumingFood);
        SubscribeLocalEvent<FleshPassiveComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(Entity<FleshPassiveComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Stomach is not { } stomach || TerminatingOrDeleted(stomach))
            return;

        QueueDel(stomach);
    }

    /* TODO SHITMED: something better wtf is this
    private void OnExclude(Entity<FleshPassiveComponent> ent, ref ExcludeMetabolismGroupsEvent args)
    {
        if (ResolveStomach(ent) is not {} stomach || args.Metabolizer == stomach)
            return;

        args.Groups ??= [];
        args.Groups.Add("Food");
        args.Groups.Add("Drink");
    }*/

    private void OnConsumingFood(Entity<FleshPassiveComponent> ent, ref ConsumingFoodEvent args)
    {
        if (args.Volume <= FixedPoint2.Zero)
            return;

        if (!Heretic.TryGetHereticComponent(ent.Owner, out var heretic, out _) || heretic.PathStage <= 0)
            return;

        var multiplier = GetMultiplier((ent.Owner, ent.Comp), heretic, ref args, out var stage, out var multipliersApplied);
        if (!multipliersApplied)
            return;

        var time = TimeSpan.FromMinutes(1) * stage;
        if (heretic.Ascended)
            time += TimeSpan.FromMinutes(1);

        ApplyMultiplier(ent, multiplier * ent.Comp.BaseHealingPerFlesh, time);
        ApplyMultiplier(ent, multiplier * ent.Comp.BaseAttackRatePerFlesh, time);
        ApplyMultiplier(ent, multiplier * ent.Comp.BaseMoveSpeedPerFlesh, time);
        _modifier.RefreshMovementSpeedModifiers(ent.Owner);
    }

    private float GetMultiplier(Entity<FleshPassiveComponent> ent,
        HereticComponent heretic,
        ref ConsumingFoodEvent args,
        out float stage,
        out bool multipliersApplied)
    {
        stage = MathF.Pow(heretic.PathStage, 0.3f);
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
        if (HasComp<HumanOrganComponent>(args.Food))
            multiplier *= ent.Comp.HumanMultiplier;
        if (heretic.Ascended)
            multiplier *= ent.Comp.AscensionMultiplier;

        multipliersApplied = oldMult < multiplier;
        return multiplier;
    }

    // Martial arts cuz yeah
    private void ApplyMultiplier(EntityUid uid, float multiplier, TimeSpan time)
    {
        if (Math.Abs(multiplier) < 0.01f || time <= TimeSpan.Zero)
            return;
    }

    private void OnMapInit(Entity<FleshPassiveComponent> ent, ref MapInitEvent args)
    {
        ResolveStomach(ent);
    }

    private EntityUid? ResolveStomach(Entity<FleshPassiveComponent> ent)
    {
        if (ent.Comp.Stomach is {} stomach)
            return stomach;

        var uid = Spawn(FleshStomach, Transform(ent).Coordinates);
        if (!_body.ReplaceOrgan(ent.Owner, uid))
        {
            // you won't have a stomach left if it failed for some reason, sorry!
            Del(uid);
            return null;
        }

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

        if (!Heretic.TryGetHereticComponent(ent.Owner, out var heretic, out _) || !heretic.Ascended)
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
            if (CreateFleshMimic(ent, ent, true, true, 50, args.Origin) is { } clone)
                ent.Comp.FleshMimics.Add(clone);
        }

        ent.Comp.TrackedDamage -= toSpawn * ent.Comp.MimicDamage;
    }

    public EntityUid? CreateFleshMimic(EntityUid uid,
        EntityUid user,
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
