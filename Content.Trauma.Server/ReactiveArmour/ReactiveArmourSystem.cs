// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Random;
using Robust.Shared.Audio.Systems;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Server.Lightning;
using Content.Goobstation.Common.Effects;
using Content.Trauma.Shared.Teleportation.Systems;
using Content.Trauma.Shared.Projectiles;
using System;


namespace Content.Trauma.Server.ReactiveArmour;

/// <summary>
/// Handles behavior of reactive armour
/// </summary>
public sealed partial class ReactiveArmourSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private RandomTeleportSystem _teleport = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private CommonSparksSystem _sparks = default!;
    [Dependency] private LightningSystem _lightning = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ReactiveArmourComponent, AttackedEvent>(OnAttakedMele);
        SubscribeLocalEvent<ReactiveArmourComponent, HitByProjectileEvent>(OnHitByProjectile);
    }

    // there has to be a better way...
    private void OnAttakedMele(Entity<ReactiveArmourComponent> ent, ref AttackedEvent args)
    {
        ChooseBehavior(ent);
    }

    private void OnHitByProjectile(Entity<ReactiveArmourComponent> ent, ref HitByProjectileEvent args)
    {
        ChooseBehavior(ent);
    }

    private void ChooseBehavior(Entity<ReactiveArmourComponent> ent)
    {
        if (ent.Comp.ArmourBehavior == null)
            return;

        var tmp = _random.NextFloat(0.0f, 1.0f);
        if (tmp < ent.Comp.ActivationChance)
            return;

        if (ent.Comp.ArmourBehavior == "Teleport"){ // bs core
            _audio.PlayPredicted(ent.Comp.DepartureSound, ent, null);
            _sparks.DoSparks(ent);

            var newCoords = _teleport.RandomTeleport(ent, ent.Comp.TeleportationRadius);

            _audio.PlayPredicted(ent.Comp.ArrivalSound, ent, null);
            _sparks.DoSparks(ent);

            _adminLog.Add(LogType.Action, LogImpact.Low, $"{ent:actor} randomly teleported to {newCoords} by reacive armour");
        }
        if (ent.Comp.ArmourBehavior == "Tesla"){ // electric core
            _lightning.ShootRandomLightnings(ent, ent.Comp.LightningRange, ent.Comp.LightningBoltCount);
            _adminLog.Add(LogType.Action, LogImpact.Low, $"{ent:actor} shoot lightnings due to wearing reacive armour");
        }
        if (ent.Comp.ArmourBehavior == "Cloack"){ // shadow core

        }
        if (ent.Comp.ArmourBehavior == "Repulsive"){ // grav core

        }

        // Reactive Incendiary Armor - Sets the wearer on fire {trollface} - pyro core
    }

}
