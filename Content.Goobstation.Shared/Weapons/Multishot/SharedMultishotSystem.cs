// SPDX-FileCopyrightText: 2024 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 BombasterDS <115770678+BombasterDS@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 BombasterDS <deniskaporoshok@gmail.com>
// SPDX-FileCopyrightText: 2025 BombasterDS2 <shvalovdenis.workmail@gmail.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 Ted Lukin <66275205+pheenty@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gluesniffler <linebarrelerenthusiast@gmail.com>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
// SPDX-FileCopyrightText: 2025 pheenty <fedorlukin2006@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Common.Weapons.Multishot;
using Content.Goobstation.Shared.Weapons.MissChance;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Goobstation.Shared.Weapons.Multishot;

public sealed class SharedMultishotSystem : EntitySystem
{
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly MissChanceSystem _miss = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;

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
        var user = args.SenderSession.AttachedEntity;

        if (user == null ||
            !_combatMode.IsInCombatMode(user))
            return;

        var gunsEnumerator = GetMultishotGuns(user.Value);
        var shootCoords = GetCoordinates(msg.Coordinates);
        var target = GetEntity(msg.Target);

        foreach(var gun in gunsEnumerator)
        {
            var (gunEnt, gunComp, _) = gun;

            if (!HasComp<MultishotComponent>(GetEntity(msg.Gun)) && gunEnt != GetEntity(msg.Gun))
                continue;

            if (gunComp.Target == null || !gunComp.BurstActivated || !gunComp.LockOnTargetBurst)
                gunComp.Target = target;

            _gun.AttemptShoot(user.Value, gunEnt, gunComp, shootCoords, gunComp.Target);
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
            gun.Item3.MultishotAffected = true;
            Dirty(gun.Item1, gun.Item3);
            _gun.RefreshModifiers(gun.Item1);
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
            gun.Item3.MultishotAffected = false;
            Dirty(gun.Item1, gun.Item3);
            _gun.RefreshModifiers(gun.Item1);
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
    private List<(EntityUid, GunComponent, MultishotComponent)> GetMultishotGuns(EntityUid entity)
    {
        var handsItems = _hands.EnumerateHeld(entity);
        var itemList = new List<(EntityUid, GunComponent, MultishotComponent)>();

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
