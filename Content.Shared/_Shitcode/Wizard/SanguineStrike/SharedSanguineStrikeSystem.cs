// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Medical.Common.Damage;
using Content.Medical.Common.Targeting;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._Goobstation.Wizard.SanguineStrike;

public abstract class SharedSanguineStrikeSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SanguineStrikeComponent, MeleeHitEvent>(OnHit);
        SubscribeLocalEvent<SanguineStrikeComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<SanguineStrikeComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("sanguine-strike-examine"));
    }

    private void OnHit(Entity<SanguineStrikeComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || args.HitEntities.Count == 0)
            return;

        if (args.HitEntities.Contains(args.User))
            return;

        var mobStateQuery = GetEntityQuery<MobStateComponent>();
        var hitMobs = args.HitEntities
            .Where(x => mobStateQuery.TryComp(x, out var mobState) && mobState.CurrentState != MobState.Dead)
            .ToList();
        if (hitMobs.Count == 0)
            return;

        var (uid, comp) = ent;

        var damageWithoutStructural = args.BaseDamage;
        damageWithoutStructural.DamageDict.Remove("Structural");
        var damage = damageWithoutStructural * comp.DamageMultiplier;
        var totalBaseDamage = damageWithoutStructural.GetTotal();
        var totalDamage = totalBaseDamage * comp.DamageMultiplier;
        if (totalDamage > 0f && totalDamage > comp.MaxDamageModifier)
        {
            damage *= comp.MaxDamageModifier / totalDamage;
            damage += damageWithoutStructural;
        }

        var newTotalDamage = damage.GetTotal();
        if (newTotalDamage > totalBaseDamage)
            args.BonusDamage += damage - damageWithoutStructural;
        args.HitSoundOverride = comp.HitSound;

        LifeSteal(args.User, newTotalDamage);

        Hit(uid, comp, args.User, hitMobs);
    }

    protected virtual void Hit(EntityUid uid,
        SanguineStrikeComponent component,
        EntityUid user,
        IReadOnlyList<EntityUid> hitEntities)
    {
    }

    public virtual void BloodSteal(EntityUid user,
        IReadOnlyList<EntityUid> hitEntities,
        FixedPoint2 bloodStealAmount,
        EntityCoordinates? bloodSpillCoordinates)
    {
    }

    public virtual void ParticleEffects(EntityUid user, IReadOnlyList<EntityUid> targets, EntProtoId particle)
    {
    }

    public void LifeSteal(Entity<DamageableComponent?> ent, FixedPoint2 amount)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var ev = new LifeStealHealEvent();
        RaiseLocalEvent(ent, ref ev);

        var totalUserDamage = ent.Comp.TotalDamage;
        if (totalUserDamage <= FixedPoint2.Zero)
            return;

        var toHeal = ent.Comp.Damage;
        if (amount < totalUserDamage)
            toHeal *= amount / totalUserDamage;

        _damageable.TryChangeDamage(ent,
            -toHeal,
            true,
            false,
            null,
            false,
            targetPart: TargetBodyPart.All,
            splitDamage: SplitDamageBehavior.SplitEnsureAll);
    }
}
