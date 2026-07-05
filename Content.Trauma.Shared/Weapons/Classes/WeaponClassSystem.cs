// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Examine;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Trauma.Shared.Knowledge.Systems;

namespace Content.Trauma.Shared.Weapons.Classes;

/// <summary>
/// Handles examining weapon's classes and their effects on combat.
/// </summary>
public sealed partial class WeaponClassSystem : EntitySystem
{
    [Dependency] private SharedKnowledgeSystem _knowledge;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WeaponClassComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<WeaponClassComponent, GetMeleeDamageEvent>(OnGetMeleeDamage);
        SubscribeLocalEvent<WeaponClassComponent, GunGetAmmoSpreadEvent>(OnGetAmmoSpread);
    }

    private void OnExamined(Entity<WeaponClassComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || !ent.Comp.Examinable)
            return;

        var name = ProtoMan.Index(ent.Comp.Class).Name;
        args.PushMarkup($"This weapon benefits from [color=green]{name}[/color] training");
    }

    private void OnGetMeleeDamage(Entity<WeaponClassComponent> ent, ref GetMeleeDamageEvent args)
    {
        var proto = ProtoMan.Index(ent.Comp.Class);
        var level = GetSkillLevel(proto, args.User);
        args.Damage *= proto.MeleeDamage.GetCurve(level);
    }

    // TODO: reduce aiming time instead of spread slop
    private void OnGetAmmoSpread(Entity<WeaponClassComponent> ent, ref GunGetAmmoSpreadEvent args)
    {
        if (args.User is not { } user)
            return; // welp

        var proto = ProtoMan.Index(ent.Comp.Class);
        var level = GetSkillLevel(proto, user);
        args.Spread *= proto.AimSpeed.GetCurve(level);
    }

    public int GetSkillLevel(Entity<WeaponClassComponent> ent, EntityUid user)
        => GetSkillLevel(ProtoMan.Index(ent.Comp.Class), user);

    public int GetSkillLevel(WeaponClassPrototype proto, EntityUid user)
        => _knowledge.GetKnowledgeLevel(user, proto.Knowledge);
}
