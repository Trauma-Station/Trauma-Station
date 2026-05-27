// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Projectiles;
using Content.Shared.Tools.Components;
using Content.Shared.Weapons.Melee;
using Content.Shared.Wieldable.Components;
using Content.Trauma.Common.Construction;
using Content.Trauma.Shared.BurnableFood;
using Content.Trauma.Shared.Durability;
using Robust.Shared.Map;

namespace Content.Trauma.Shared.Forging;

/// <summary>
/// Handles forged item procedural generation.
/// </summary>
public sealed partial class ForgingSystem : EntitySystem
{
    [Dependency] private DurabilitySystem _durability = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private MetaDataSystem _meta = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedMetalSystem _metal = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private WorkableSystem _workable = default!;
    [Dependency] private EntityQuery<ForgedItemComponent> _query = default!;

    public static readonly EntProtoId UnfinishedItem = "UnfinishedForgedItem";
    public static readonly EntProtoId DefaultResult = "ForgedPart";

    /// <summary>
    /// Cache of all forged items, grouped by their category.
    /// </summary>
    public Dictionary<ForgingCategoryPrototype, List<ForgedItemPrototype>> AllItems = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ForgedItemComponent, MetalWroughtEvent>(OnWrought);

        SubscribeLocalEvent<MeleeWeaponComponent, ForgingCompletedEvent>(OnMeleeCompleted);
        SubscribeLocalEvent<DamageOtherOnHitComponent, ForgingCompletedEvent>(OnDamageOtherCompleted);
        SubscribeLocalEvent<IncreaseDamageOnWieldComponent, ForgingCompletedEvent>(OnDamageWieldCompleted);
        SubscribeLocalEvent<ProjectileComponent, ForgingCompletedEvent>(OnProjectileCompleted);
        SubscribeLocalEvent<ToolComponent, ForgingCompletedEvent>(OnToolCompleted);

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        LoadPrototypes();
    }

    private void OnWrought(Entity<ForgedItemComponent> ent, ref MetalWroughtEvent args)
    {
        var metal = _metal.GetMetalOrThrow(ent.Owner);
        var item = _proto.Index(ent.Comp.Item);
        var uid = args.Result;
        // for procgen colour and stuff
        _metal.SetMetal(uid, metal);
        SetItemProto(uid, ent.Comp.Item, completed: true);
        var metalProto = _proto.Index(metal);
        MakeOverheatable(uid, metalProto, completed: true);
        var itemName = item.DisplayName(_proto);
        ModifyResult(uid, args.User, metalProto, item, itemName);
        if (item.Tag is {} tag)
            _metal.AddUnworkableTag(uid, tag); // added once it cools down
    }

    private void OnMeleeCompleted(Entity<MeleeWeaponComponent> ent, ref ForgingCompletedEvent args)
    {
        if (args.Metal.Speed != 1f)
        {
            ent.Comp.AttackRate *= args.Metal.Speed;
            DirtyField(ent, ent.Comp, nameof(MeleeWeaponComponent.AttackRate));
        }

        if (args.Metal.Damage.Count == 0)
            return;

        ModifyDamage(ent.Comp.Damage.DamageDict, args.Metal.Damage);
        DirtyField(ent, ent.Comp, nameof(MeleeWeaponComponent.Damage));
    }

    private void OnDamageOtherCompleted(Entity<DamageOtherOnHitComponent> ent, ref ForgingCompletedEvent args)
    {
        if (args.Metal.Damage.Count == 0)
            return;

        ModifyDamage(ent.Comp.Damage.DamageDict, args.Metal.Damage);
        Dirty(ent);
    }

    private void OnDamageWieldCompleted(Entity<IncreaseDamageOnWieldComponent> ent, ref ForgingCompletedEvent args)
    {
        if (args.Metal.Damage.Count == 0)
            return;

        ModifyDamage(ent.Comp.BonusDamage.DamageDict, args.Metal.Damage);
        Dirty(ent);
    }

    private void OnProjectileCompleted(Entity<ProjectileComponent> ent, ref ForgingCompletedEvent args)
    {
        if (args.Metal.Damage.Count == 0)
            return;

        ModifyDamage(ent.Comp.Damage.DamageDict, args.Metal.Damage);
        Dirty(ent);
    }

    private void OnToolCompleted(Entity<ToolComponent> ent, ref ForgingCompletedEvent args)
    {
        if (args.Metal.Speed == 1f)
            return;

        ent.Comp.SpeedModifier *= args.Metal.Speed;
        Dirty(ent);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<ForgedItemPrototype>() || args.WasModified<ForgingCategoryPrototype>())
            LoadPrototypes();
    }

    private void LoadPrototypes()
    {
        AllItems.Clear();

        foreach (var category in _proto.EnumeratePrototypes<ForgingCategoryPrototype>())
        {
            AllItems[category] = new();
        }

        foreach (var item in _proto.EnumeratePrototypes<ForgedItemPrototype>())
        {
            var category = _proto.Index(item.Category);
            AllItems[category].Add(item);
        }

        foreach (var items in AllItems.Values)
        {
            items.Sort((a, b) => a.Name.CompareTo(b.Name));
        }
    }

    private void ModifyDamage(Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2> damage, Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2> modifiers)
    {
        foreach (var (type, modifier) in modifiers)
        {
            if (damage.TryGetValue(type, out var old))
                damage[type] = old * modifier;
        }
    }

    public EntityUid SpawnUnfinished(EntityCoordinates coords,
        [ForbidLiteral] ProtoId<MetalPrototype> metal,
        [ForbidLiteral] ProtoId<ForgedItemPrototype> item,
        FixedPoint2 workScale)
    {
        var uid = PredictedSpawnAtPosition(UnfinishedItem, coords);
        _transform.SetLocalRotation(uid, 0); // dogshit engine decision award

        _metal.SetMetal(uid, metal);
        SetItemProto(uid, item);
        var metalProto = _proto.Index(metal);
        MakeOverheatable(uid, metalProto);
        var metalName = metalProto.Name;
        var itemProto = _proto.Index(item);
        var itemName = itemProto.DisplayName(_proto);
        _meta.SetEntityName(uid, $"unfinished {metalName} {itemName}");

        // actually let the result be made by working it
        var workable = Comp<WorkableComponent>(uid);
        var work = itemProto.Work * metalProto.WorkScale * workScale;
        _workable.SetRemaining((uid, workable), work);
        _workable.SetResult((uid, workable), itemProto.Result ?? DefaultResult);
        _workable.SetAmount((uid, workable), itemProto.Amount);
        // TODO: other shit?
        return uid;
    }

    /// <summary>
    /// Set the item prototype for a procedurally generated item.
    /// </summary>
    public void SetItemProto(EntityUid uid, [ForbidLiteral] ProtoId<ForgedItemPrototype> item, bool completed = false)
    {
        var comp = EnsureComp<ForgedItemComponent>(uid);
        comp.Item = item;
        comp.Completed = completed;
        Dirty(uid, comp);

        // let client update sprite and server set the temperature
        var ev = new ItemForgedEvent(item);
        RaiseLocalEvent(uid, ref ev);
    }

    /// <summary>
    /// Make an unfinished piece able to turn brittle and require reblooming, or a finished piece melt.
    /// </summary>
    public void MakeOverheatable(EntityUid uid, MetalPrototype metal, bool completed = false)
    {
        // TODO: this needs a better name...
        var comp = EnsureComp<BurnableFoodComponent>(uid);
        comp.BurnTemp = completed ? metal.MeltTemp : metal.MaxTemp;
        // TODO: melt into molten metal instead of brittle scraps
        comp.BurnedFoodPrototype = metal.Overheated;
        comp.BurnedPrefix = "heated-name-text";
        comp.BurnedPopup = completed ? "metal-melted-popup" : "workable-metal-overheat-popup";
    }

    public void ModifyResult(EntityUid uid, EntityUid? user, MetalPrototype metal, ForgedItemPrototype item, string itemName)
    {
        _meta.SetEntityName(uid, $"{metal.Name} {itemName}");

        // change stats of the resulting item
        _durability.SetScale(uid, metal.Durability);
        var ev = new ForgingCompletedEvent(metal, item, uid, user);
        RaiseLocalEvent(uid, ref ev);
        if (user != null)
            RaiseLocalEvent(user.Value, ref ev);
    }

    /// <summary>
    /// Finish a constructed item, changing it into the item's finished prototype.
    /// </summary>
    public void FinishForgedItem(EntityUid part, EntityUid? user)
    {
        if (!_query.TryComp(part, out var comp))
        {
            Log.Error($"Tried to finish non-forged item {ToPrettyString(part)}!");
            return;
        }

        var item = _proto.Index(comp.Item);
        if (item.Finished is not {} finished)
        {
            Log.Error($"Forged item {comp.Item} from {ToPrettyString(part)} did not have a finished prototype!");
            return;
        }

        var wasHolding = user != null && _hands.IsHolding(user.Value, part);

        var xform = Transform(part);
        var rot = xform.LocalRotation;
        var uid = Spawn(finished, xform.Coordinates);
        var metal = _proto.Index(_metal.GetMetalOrThrow(part));
        _metal.SetMetal(uid, metal); // TODO: completely modular weapons, dont delete the original and just use it for visuals by composing sprite layers
        ModifyResult(uid, user, metal, item, Name(uid));
        QueueDel(part);

        _transform.AttachToGridOrMap(uid);
        _transform.SetLocalRotation(uid, rot);

        if (wasHolding)
            _hands.TryPickupAnyHand(user!.Value, uid);

        var ev = new ConstructionChangedEvent(uid);
        RaiseLocalEvent(part, ref ev);
    }

    /// <summary>
    /// Returns true if a forged item prototype can be made from a given metal.
    /// </summary>
    public bool CanMakeFrom(ForgedItemPrototype item, [ForbidLiteral] ProtoId<MetalPrototype> metal)
        => item.Whitelist?.Contains(metal) != false &&
            item.Blacklist?.Contains(metal) != true;
}
