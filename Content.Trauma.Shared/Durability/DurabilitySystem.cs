// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Damage.Components;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Materials;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Stacks;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Trauma.Shared.Durability.Components;
using Content.Trauma.Shared.Durability.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Trauma.Shared.Durability;

public sealed class DurabilitySystem : EntitySystem
{
    [Dependency] private readonly SharedDestructibleSystem _destructible = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private static readonly Dictionary<DurabilityState, Color> AssociatedColors = new()
    {
        {DurabilityState.Reinforced, new Color(98, 217, 195)},
        {DurabilityState.Pristine, new Color(117, 217, 98)},
        {DurabilityState.Worn, new Color(217, 191, 98)},
        {DurabilityState.Damaged, new Color(217, 140, 98)},
        {DurabilityState.Broken, new Color(217, 98, 98)},
        {DurabilityState.Destroyed, Color.Red},
    };

    private const string ExamineTextColor = "durability-repair-colortext";
    private const string ToolQualityPrefix = "durability-tool-";
    private const string ExamineTextRepairReqs = "durability-repair-needed";
    private static readonly HashSet<MaterialPrototype> ExamineMats = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DurabilityComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<DurabilityComponent, AttemptMeleeEvent>(OnAttemptMelee);
        SubscribeLocalEvent<DurabilityComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<DurabilityComponent, GetMeleeDamageEvent>(OnGetMeleeDamage);
        SubscribeLocalEvent<DurabilityComponent, AttemptShootEvent>(OnAttemptShoot);
        SubscribeLocalEvent<DurabilityComponent, GunShotEvent>(OnGunShot);
        SubscribeLocalEvent<DurabilityComponent, GunRefreshModifiersEvent>(OnGunRefreshModifiers);
        SubscribeLocalEvent<DurabilityComponent, DurabilityDamageChangedEvent>(OnDurabilityDamageChanged);
        SubscribeLocalEvent<GunComponent, DurabilityStateChangedEvent>(OnStateChangeGun);
        SubscribeLocalEvent<DurabilityComponent, DurabilityStateChangedEvent>(OnDurabilityStateChanged);
        SubscribeLocalEvent<DurabilityComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<DurabilityComponent, RepairItemDoAfterEvent>(OnRepairItemDoAfter);
        SubscribeLocalEvent<DurabilityComponent, RepairToolDoAfterEvent>(OnRepairToolDoAfter);
    }

    public bool DamageEntity(EntityUid uid, FixedPoint2 amount, DurabilityComponent? comp = null, EntityUid? attacker = null, HashSet<EntityUid>? targets = null)
    {
        if (!Resolve(uid, ref comp))
            return false;
        //Dealing negative damage should always succeed since well, that's a positive effect.
        if (Math.Sign(amount.Value) > 0 && !RollDamageChance(uid, comp))
            return false;

        // Check if anything may end up negating the damage. Negative damage heals, obviously.
        var beforeEv = new DurabilityChangeAttemptEvent(uid, amount);
        RaiseLocalEvent(uid, ref beforeEv);
        amount = beforeEv.Damage;
        var oldDamage = comp.Damage;
        comp.Damage += amount;
        if (comp.Damage < -comp.MaxRepairBonus)
            comp.Damage = -comp.MaxRepairBonus; // cap lower bound

        var oldState = comp.DurabilityState;
        comp.DurabilityState = GetDurabilityState(comp);
        Dirty(uid, comp);
        // Don't raise the event if it didn't actually change.
        if (comp.DurabilityState != oldState)
        {
            var stateEv = new DurabilityStateChangedEvent(oldState, comp.DurabilityState, uid, attacker, targets);
            RaiseLocalEvent(uid, ref stateEv);
        }

        var afterEv = new DurabilityDamageChangedEvent(uid, comp.Damage, oldDamage);
        RaiseLocalEvent(uid, ref afterEv);
        return oldDamage != comp.Damage;
    }

    private bool RollDamageChance(EntityUid uid, DurabilityComponent comp)
    {
        return SharedRandomExtensions.PredictedProb(_timing,
            Math.Clamp(comp.DamageProbability, 0, 1),
            GetNetEntity(uid));
    }

    private DurabilityState GetDurabilityState(DurabilityComponent comp)
    {
        foreach (var (threshold, durabilityState) in comp.DurabilityThresholds.Reverse())
        {
            // handle reinforced if not defined
            if (durabilityState is DurabilityState.Pristine &&
                !comp.DurabilityThresholds.ContainsValue(DurabilityState.Reinforced) && comp.Damage < 0)
                return DurabilityState.Reinforced;

            if (comp.Damage < threshold)
                continue;

            return durabilityState;
        }

        return DurabilityState.Pristine;
    }

    private FixedPoint2 GetDurabilityModifier(DurabilityComponent comp)
    {
        if (!comp.DurabilityModifiers.TryGetValue(comp.DurabilityState, out var mod))
            return comp.DurabilityState is DurabilityState.Destroyed ? 0 : 1;
        return mod;
    }

    // Hello welcome to the super turbo shitcode inc™ string builder function of doom and gloom.
    private List<string> GetRepairMaterialString(DurabilityComponent comp)
    {
        ExamineMats.Clear();
        foreach (var material in comp.RepairMaterials.Keys)
        {
            if (!_proto.Resolve(material, out var proto))
                continue;
            ExamineMats.Add(proto);
        }

        if (comp.RepairTool is null && ExamineMats.Count == 0)
            // ReSharper disable once UseCollectionExpression | literally cant, client no likey
            return new List<string> {Loc.GetString("durability-repair-irreparable")};
        var start = (ExamineMats.Count == 1 && comp.RepairTool is null) || (ExamineMats.Count == 0 && comp.RepairTool is not null)
            ? "durability-repair-single"
            : "durability-repair-multiple";
        // ReSharper disable once UseCollectionExpression | shut the fuck up I CANNNTTTTTT
        List<string> entries = new(){start};

        if (comp.RepairTool is {} tool)
        {
            entries.Add(Loc.GetString(ExamineTextColor,
                ("data", Loc.GetString($"{ToolQualityPrefix}{tool.Id.ToLower()}"))));
        }

        entries.AddRange(ExamineMats.Select(material => Loc.GetString(ExamineTextColor, ("data", $"{Loc.GetString(material.Name)} {Loc.GetString(material.Unit)}"))));

        // only one entry was added, first entry is just the starting text
        if (entries.Count == 2)
        {
            var dashIdx = entries[1].IndexOf("- ", StringComparison.Ordinal); // mega poo-poo stinky shitcode but unless i want to spend 2 hours rewriting this whole function, it stays.
            // ReSharper disable once UseCollectionExpression | SHUT UUUUPPPPP
            entries = new List<string>
            {
                $"{Loc.GetString(ExamineTextRepairReqs, ("requirements", Loc.GetString(entries[0])))}{entries[1].Remove(dashIdx, 2)}",
            };
            return entries;
        }

        entries[0] = Loc.GetString(ExamineTextRepairReqs, ("requirements", Loc.GetString(entries[0])));
        return entries;
    }

    private void OnExamined(Entity<DurabilityComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup("durability"))
        {
            args.PushMarkup(Loc.GetString("durability-examine-condition",
                ("color", AssociatedColors[ent.Comp.DurabilityState].ToHex()),
                ("state", ent.Comp.DurabilityState.ToString())));

            // only show if it even has melee damage
            if (HasComp<MeleeWeaponComponent>(ent))
            {
                args.PushMarkup(Loc.GetString("durability-examine-weapon",
                    ("color", AssociatedColors[ent.Comp.DurabilityState].ToHex()),
                    ("mod", GetDurabilityModifier(ent.Comp))));
            }

            // only show if it even has gun values like this
            if (HasComp<GunComponent>(ent))
            {
                args.PushMarkup(Loc.GetString("durability-examine-gun",
                    ("color", AssociatedColors[ent.Comp.DurabilityState].ToHex()),
                    ("mod", GetDurabilityModifier(ent.Comp))));
            }

            var entries = GetRepairMaterialString(ent.Comp);
            foreach (var entry in entries)
            {
                args.PushMarkup(entry);
            }
        }
    }

    private void OnAttemptMelee(Entity<DurabilityComponent> ent, ref AttemptMeleeEvent args)
    {
        // Prohibit attacking with a destroyed weapon; it is in such a state of disrepair that it cannot be used.
        if (ent.Comp.DurabilityState is not DurabilityState.Destroyed)
            return;
        args.Cancelled = true;
        if (ent.Comp.DestroyedSwingAttemptPopup.HasValue)
            args.Message = Loc.GetString(ent.Comp.DestroyedSwingAttemptPopup, ("weapon", Name(ent.Owner)));
    }

    private void OnMeleeHit(Entity<DurabilityComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        // Check if anything can even take damage here. You really shouldn't lose durability for misclicking a puddle or something.
        if (!args.HitEntities.Any(HasComp<DamageableComponent>))
            return;

        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent.Owner));
        var damage = random.NextFloat(ent.Comp.MinDamageRoll.Float(), ent.Comp.MaxDamageRoll.Float());
        DamageEntity(ent.Owner, damage, ent.Comp, args.User, args.HitEntities.ToHashSet());
    }

    private void OnGetMeleeDamage(Entity<DurabilityComponent> ent, ref GetMeleeDamageEvent args)
    {
        args.Damage *= GetDurabilityModifier(ent.Comp);
    }

    private void OnAttemptShoot(Entity<DurabilityComponent> ent, ref AttemptShootEvent args)
    {
        if (ent.Comp.DurabilityState is not DurabilityState.Destroyed)
            return;
        args.Cancelled = true;
        if (ent.Comp.DestroyedSwingAttemptPopup.HasValue)
            args.Message = Loc.GetString(ent.Comp.DestroyedSwingAttemptPopup, ("weapon", Name(ent.Owner)));
    }

    private void OnGunShot(Entity<DurabilityComponent> ent, ref GunShotEvent args)
    {
        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent.Owner));
        var damage = random.NextFloat(ent.Comp.MinDamageRoll.Float(), ent.Comp.MaxDamageRoll.Float());
        DamageEntity(ent.Owner, damage, ent.Comp, args.User); // targets not applicable
    }

    private void OnGunRefreshModifiers(Entity<DurabilityComponent> ent, ref GunRefreshModifiersEvent args)
    {
        var mod = GetDurabilityModifier(ent.Comp).Float();
        args.FireRate *= mod;
        args.BurstFireRate *= mod;
        args.MaxAngle /= mod;
        args.MinAngle /= mod;
        args.AngleDecay *= mod;
        args.AngleIncrease *= mod;
        args.BurstCooldown /= mod;
    }

    private void OnDurabilityDamageChanged(Entity<DurabilityComponent> ent, ref DurabilityDamageChangedEvent args)
    {
        var diff = args.Damage - args.OldDamage;

        switch (Math.Sign(diff.Value))
        {
            case < 0:
            {
                var locId = args.OldDamage <= 0 && args.Damage <= 0 ? "durability-reinforce-popup" : "durability-repair-popup";
                var amount = args.OldDamage - FixedPoint2.Max(args.Damage, -ent.Comp.MaxRepairBonus);
                _popup.PopupPredictedCoordinates(
                    Loc.GetString(locId, ("weapon", Name(ent.Owner)), ("amount", amount)),
                    Transform(ent.Owner).Coordinates,
                    null);
                break;
            }
            case > 0:
            {
                if (!ent.Comp.DamagePopups.TryGetValue(ent.Comp.DurabilityState, out var pool))
                    return;
                var locId = _random.Pick(pool);
                _popup.PopupPredictedCoordinates(Loc.GetString(locId),
                    Transform(ent.Owner).Coordinates,
                    null,
                    PopupType.SmallCaution);
                break;
            }
            case 0 when ent.Comp.Damage <= -ent.Comp.MaxRepairBonus:
            {
                _popup.PopupPredictedCoordinates(
                    Loc.GetString("durability-repair-max", ("weapon", Name(ent.Owner))),
                    Transform(ent.Owner).Coordinates,
                    null);
                break;
            }
        }
    }

    private void OnDurabilityStateChanged(Entity<DurabilityComponent> ent, ref DurabilityStateChangedEvent args)
    {
        if (args.NewState is not DurabilityState.Destroyed)
            return;

        ent.Comp.OnBreakBehavior?.Execute(ent.Owner, _destructible);
        if (!ent.Comp.DeleteOnDestroyed)
            return;
        PredictedQueueDel(ent.Owner);

        if (TryComp<MeleeWeaponComponent>(args.Attacker, out var userMelee))
        {
            userMelee.NextAttack = _timing.CurTime + TimeSpan.FromSeconds(1 / userMelee.AttackRate);
            Dirty(args.Attacker.Value, userMelee);
        }
    }

    private void OnStateChangeGun(Entity<GunComponent> ent, ref DurabilityStateChangedEvent args)
    {
        // guns need to refresh modifiers
        _gun.RefreshModifiers(ent.AsNullable(), args.Attacker);
    }

    private void OnInteractUsing(Entity<DurabilityComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Target != ent.Owner || args.Handled)
            return;

        if (TryComp<ToolComponent>(args.Used, out var tool) && ent.Comp.RepairTool is not null)
        {
            if (_tool.HasQuality(args.Used, ent.Comp.RepairTool, tool))
            {
                _tool.UseTool(ent.Owner,
                    args.User,
                    args.Target,
                    ent.Comp.RepairDoAfter,
                    [ent.Comp.RepairTool],
                    new RepairToolDoAfterEvent(),
                    out _,
                    ent.Comp.FuelCost,
                    tool);
                args.Handled = true;
                return;
            }
            // fall through to see if it is an accepted material
        }

        if (!HasComp<MaterialComponent>(args.Used))
            return;
        if (!TryComp<PhysicalCompositionComponent>(args.Used, out var composition))
            return;

        var minmax = ent.Comp.RepairMaterials
            .Where(kvp => composition.MaterialComposition.ContainsKey(kvp.Key))
            .Select(kvp => kvp.Value)
            .FirstOrNull();

        if (minmax is null)
            return;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            args.User,
            ent.Comp.RepairDoAfter,
            new RepairItemDoAfterEvent(minmax.Value),
            ent.Owner,
            args.Target,
            args.Used));
        args.Handled = true;
    }

    private void OnRepairItemDoAfter(Entity<DurabilityComponent> ent, ref RepairItemDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || Deleted(args.Used))
            return;

        var (min, max) = args.MinMax;

        // deal negative damage to heal
        if (!DamageEntity(ent.Owner,
                -SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent.Owner)).NextFloat(min, max),
                ent.Comp))
            return;

        if (TryComp<StackComponent>(args.Used, out var stack))
            _stack.ReduceCount((args.Used.Value, stack), 1);
        else
            PredictedQueueDel(args.Used);

        args.Handled = true;
    }

    private void OnRepairToolDoAfter(Entity<DurabilityComponent> ent, ref RepairToolDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || Deleted(args.Used))
            return;

        if (ent.Comp.RepairTool is null)
            return;

        var (min, max) = ent.Comp.ToolRepairAmount;

        DamageEntity(ent.Owner,
            -SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent.Owner)).NextFloat(min, max),
            ent.Comp);

        _tool.PlayToolSound(args.Used.Value, Comp<ToolComponent>(args.Used.Value), args.User);

        args.Handled = true;
    }
}

[Serializable, NetSerializable]
public enum DurabilityState : sbyte
{
    Reinforced = -1,
    Pristine = 0,
    Worn = 1,
    Damaged = 2,
    Broken = 3,
    Destroyed = 4,
}
