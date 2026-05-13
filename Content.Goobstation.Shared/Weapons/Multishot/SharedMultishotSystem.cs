// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Common.Weapons.Multishot;
using Content.Goobstation.Shared.Weapons.MissChance;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Utility;

namespace Content.Goobstation.Shared.Weapons.Multishot;

public sealed partial class SharedMultishotSystem : EntitySystem
{
    [Dependency] private SharedCombatModeSystem _combatMode = default!;
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private MissChanceSystem _miss = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MultishotComponent, GotEquippedHandEvent>(OnEquipWeapon);
        SubscribeLocalEvent<MultishotComponent, GotUnequippedHandEvent>(OnUnequipWeapon);
        SubscribeLocalEvent<MultishotComponent, GunRefreshModifiersEvent>(OnRefreshModifiers);
        SubscribeLocalEvent<MultishotComponent, AmmoShotEvent>(OnAmmoShot);
        SubscribeLocalEvent<MultishotComponent, ExaminedEvent>(OnExamined);
        SubscribeAllEvent<RequestShootEvent>(OnRequestShoot);
    }

    private void OnRequestShoot(RequestShootEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } user ||
            !_combatMode.IsInCombatMode(user))
            return;

        var gunsEnumerator = GetMultishotGuns(user);
        var shootCoords = GetCoordinates(msg.Coordinates);
        var target = GetEntity(msg.Target);

        foreach (var gun in gunsEnumerator)
        {
            if (!HasComp<MultishotComponent>(GetEntity(msg.Gun)) && gun.Owner != GetEntity(msg.Gun))
                continue;

            var comp = gun.Comp1;
            if (comp.Target == null || !comp.BurstActivated || !comp.LockOnTargetBurst)
                comp.Target = target;

            _gun.AttemptShoot(user, (gun.Owner, comp), shootCoords, comp.Target);
        }
    }

    private void OnAmmoShot(EntityUid uid, MultishotComponent comp, ref AmmoShotEvent args)
    {
        if (!comp.MultishotAffected)
            return;

        args.FiredProjectiles.ForEach(ent => _miss.ApplyMissChance(ent, comp.MissChance));
    }

    private void OnRefreshModifiers(EntityUid uid, MultishotComponent comp, ref GunRefreshModifiersEvent args)
    {
        if (!comp.MultishotAffected)
            return;

        args.MaxAngle = args.MaxAngle * comp.SpreadMultiplier + Angle.FromDegrees(comp.SpreadAddition);
        args.MinAngle = args.MinAngle * comp.SpreadMultiplier + Angle.FromDegrees(comp.SpreadAddition);
    }

    private void OnEquipWeapon(Entity<MultishotComponent> multishotWeapon, ref GotEquippedHandEvent args)
    {
        var gunsEnumerator = GetMultishotGuns(args.User);

        if (gunsEnumerator.Count < 2)
            return;

        foreach (var gun in gunsEnumerator)
        {
            gun.Comp2.MultishotAffected = true;
            Dirty(gun, gun.Comp2);
            _gun.RefreshModifiers((gun.Owner, gun.Comp1));
        }
    }

    private void OnUnequipWeapon(Entity<MultishotComponent> multishotWeapon, ref GotUnequippedHandEvent args)
    {
        var gunsEnumerator = GetMultishotGuns(args.User);

        multishotWeapon.Comp.MultishotAffected = false;
        _gun.RefreshModifiers(multishotWeapon.Owner);
        Dirty(multishotWeapon);

        if (gunsEnumerator.Count >= 2)
            return;

        foreach (var gun in gunsEnumerator)
        {
            gun.Comp2.MultishotAffected = false;
            Dirty(gun, gun.Comp2);
            _gun.RefreshModifiers((gun.Owner, gun.Comp1));
        }
    }

    private void OnExamined(Entity<MultishotComponent> ent, ref ExaminedEvent args)
    {
        var message = new FormattedMessage();
        var chance = (MathF.Round(ent.Comp.MissChance * 100f)).ToString();
        message.AddText(Loc.GetString(ent.Comp.ExamineMessage, ("chance", chance)));
        args.PushMessage(message);
    }

    /// <summary>
    /// Return list of guns in hands
    /// </summary>
    private List<Entity<GunComponent, MultishotComponent>> GetMultishotGuns(EntityUid entity)
    {
        var handsItems = _hands.EnumerateHeld(entity);
        var itemList = new List<Entity<GunComponent, MultishotComponent>>();

        if (!handsItems.Any())
            return itemList;

        foreach (var item in handsItems)
        {
            if (TryComp<GunComponent>(item, out var gunComp) && TryComp<MultishotComponent>(item, out var multishotComp))
                itemList.Add((item, gunComp, multishotComp));
        }

        return itemList;
    }
}
