using Content.Shared._Shitmed.Medical.Surgery.Wounds.Components;
using Content.Shared._Shitmed.Medical.Surgery.Wounds.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.Body.Prototypes;
using Content.Shared.Body.Systems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Polymorph;
using Content.Shared.Standing;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Trauma.Shared.Shapeshift;

/// <summary>
/// Shapeshift makes body->body polymorph preserve surgically installed organs and limbs, etc.
/// Using the polymorph API will automatically invoke it.
/// </summary>
public sealed partial class ShapeshiftSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ItemSlotsSystem _slots = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly WoundSystem _wound = default!;

    private EntityQuery<BodyComponent> _bodyQuery;
    private EntityQuery<BodyPartComponent> _partQuery;
    private EntityQuery<OrganComponent> _organQuery;

    private List<Entity<WoundComponent>> _wounds = new();

    public override void Initialize()
    {
        base.Initialize();

        _bodyQuery = GetEntityQuery<BodyComponent>();
        _partQuery = GetEntityQuery<BodyPartComponent>();
        _organQuery = GetEntityQuery<OrganComponent>();

        SubscribeLocalEvent<BodyComponent, PolymorphedEvent>(OnPolymorphed);

        SubscribeLocalEvent<BodyPartComponent, ShapeshiftedEvent>(OnPartShapeshifted);
        SubscribeLocalEvent<WoundableComponent, ShapeshiftedEvent>(OnWoundableShapeshifted);
    }

    private void OnPolymorphed(Entity<BodyComponent> ent, ref PolymorphedEvent args)
    {
        // don't do the swap twice
        if (args.OldEntity != ent.Owner)
            return;

        // can't do anything if you get turned into bread
        if (!_bodyQuery.TryComp(args.NewEntity, out var newBody))
            return;

        Shapeshift(ent, (args.NewEntity, newBody));
    }

    /// <summary>
    /// Swaps the artifical body parts and organs etc with a new body.
    /// </summary>
    public void Shapeshift(Entity<BodyComponent> old, Entity<BodyComponent> target)
    {
        // save mind for later
        //var mind = _mind.GetMind(old.Owner);

        // swapping legs could down you for no reason
        var standing = !_standing.IsDown(old.Owner);

        TheSwap(old, target);

        if (standing)
            _standing.Stand(target.Owner);

        // if the brain was replaced, need to re control the body
        /*if (mind is {} mindUid)
            _mind.TransferTo(mindUid, target.Owner);*/
    }

    #region THE SWAP

    // IAdminAbuseManager.TheSwap
    private void TheSwap(Entity<BodyComponent> old, Entity<BodyComponent> target)
    {
        if (!_body.TryGetRootPart(old, out var rootPart))
        {
            Log.Error($"Tried to shapeshift body {ToPrettyString(old)} into {ToPrettyString(target)} but it had no root part!");
            return;
        }

        Log.Debug($"Shapeshifting {ToPrettyString(old)} into {ToPrettyString(target)}");

        var oldProto = _proto.Index(old.Comp.Prototype);
        var newProto = _proto.Index(target.Comp.Prototype);
        // recursively swap out body parts
        TheSwap(target, oldProto, newProto, rootPart.Value);
    }

    private void TheSwap(Entity<BodyComponent> target, BodyPrototype oldProto, BodyPrototype newProto, Entity<BodyPartComponent> part)
    {
        var slot = _body.GetSlotFromBodyPart(part);
        if (!oldProto.Slots.TryGetValue(slot, out var def))
        {
            // slot doesn't exist on the prototype so add the slot and the part
            if (FindPart(target, part.Comp.ParentSlot?.Id) is {} mirrorParent)
            {
                _body.CreatePartSlot(mirrorParent, slot, part.Comp.PartType, part.Comp.Symmetry, mirrorParent.Comp);
                _body.AttachPart(mirrorParent, slot, part, mirrorParent.Comp, part.Comp);
            }
            else
            {
                Log.Error($"Tried to shapeshift {ToPrettyString(target)} from {oldProto} to {newProto} which have different root parts??");
            }
            return; // swapped the part, no point checking children
        }

        if (!newProto.Slots.ContainsKey(slot))
            return; // don't e.g. add mouse combined legs to a human with individual legs

        // the slot exists but the part might still be unique
        if (Prototype(part)?.ID is {} id && id != def.Part)
        {
            if (FindPart(target, part.Comp.ParentSlot?.Id) is {} mirrorParent)
                _body.AttachPart(mirrorParent, slot, part, mirrorParent.Comp, part.Comp);
            else
                Log.Error($"Tried to shapeshift {ToPrettyString(target)} from {oldProto} to {newProto} which had no part for {part.Comp.ParentSlot}!");
            return; // swapped the part, no point checking children
        }

        // non-unique part, check the mirror part on the new body
        if (FindPart(target, slot) is not {} mirror)
            return;

        var ev = new ShapeshiftedEvent(target, mirror);
        RaiseLocalEvent(part, ref ev);

        // find parts or organs missing from the old prototype
        // then remove them from the new one
        // i.e. if you remove someones liver and turn them into a mouse, the mouse doesn't regrow a liver
        foreach (var partSlot in def.Connections)
        {
            if (GetPartChild(part, partSlot) == null && GetPartChild(mirror, partSlot) is {} mirrorChild)
            {
                _body.DetachPart(mirror, partSlot, mirrorChild, mirror.Comp, mirrorChild.Comp);
                Del(mirrorChild);
            }
        }

        foreach (var organSlot in def.Organs.Keys)
        {
            if (GetPartOrgan(part, organSlot) == null && GetPartOrgan(mirror, organSlot) is {} mirrorOrgan)
            {
                _body.RemoveOrgan(mirrorOrgan, mirror);
                Del(mirrorOrgan);
            }
        }

        // if any extra organs or slots are found then mirror it to the new part
        foreach (var organSlot in part.Comp.Organs.Keys)
        {
            if (!def.Organs.TryGetValue(organSlot, out var defaultOrgan))
                _body.CreateOrganSlot((mirror, mirror.Comp), organSlot);

            if (GetPartOrgan(part, organSlot) is not {} organ)
                continue;

            var mirrorOrgan = GetPartOrgan(mirror, organSlot);
            if (mirrorOrgan != null)
            {
                ev = new ShapeshiftedEvent(target, mirrorOrgan.Value);
                RaiseLocalEvent(organ, ref ev);
            }

            var organId = Prototype(organ)?.ID;
            if (organId == defaultOrgan) // don't transfer default organs
                continue;

            if (mirrorOrgan != null)
            {
                _body.RemoveOrgan(mirrorOrgan.Value, mirror);
                Del(mirrorOrgan);
            }

            var inserted = _body.InsertOrgan(part, organ, organSlot, part.Comp, organ.Comp);
            if (!inserted)
                Log.Error($"Failed to insert organ {ToPrettyString(organ)} into {ToPrettyString(mirror)}!");
        }

        // if any extra parts or slots are found then mirror it to the new part
        foreach (var partSlot in part.Comp.Children.Values)
        {
            if (!def.Connections.Contains(partSlot.Id))
                _body.CreatePartSlot(mirror, partSlot.Id, partSlot.Type, partSlot.Symmetry, mirror.Comp);

            if (GetPartChild(part, partSlot.Id) is not {} child)
                continue;

            // recurse
            TheSwap(target, oldProto, newProto, child);
        }
    }

    #endregion

    #region Body helpers

    public Entity<BodyPartComponent>? FindPart(Entity<BodyComponent> body, string? slot)
    {
        if (slot == null)
            return null;

        foreach (var (uid, part) in _body.GetBodyChildren(body, body.Comp))
        {
            if (_body.GetSlotFromBodyPart(part) == slot)
                return (uid, part);
        }

        return null;
    }

    public Entity<OrganComponent>? GetPartOrgan(EntityUid part, string slot)
    {
        var containerId = SharedBodySystem.GetOrganContainerId(slot);
        if (!_container.TryGetContainer(part, containerId, out var container))
            return null;

        foreach (var organ in container.ContainedEntities)
        {
            if (_organQuery.TryComp(organ, out var comp))
                return (organ, comp);
        }

        return null;
    }

    public Entity<BodyPartComponent>? GetPartChild(EntityUid part, string slot)
    {
        var containerId = SharedBodySystem.GetPartSlotContainerId(slot);
        if (!_container.TryGetContainer(part, containerId, out var container))
            return null;

        foreach (var child in container.ContainedEntities)
        {
            if (_partQuery.TryComp(child, out var comp))
                return (child, comp);
        }

        return null;
    }

    #endregion

    #region Event handlers

    private void OnPartShapeshifted(Entity<BodyPartComponent> ent, ref ShapeshiftedEvent args)
    {
        // transfer chest cavity implant
        if (ent.Comp.ItemInsertionSlot.Item is not {} item)
            return;

        var part = _partQuery.Comp(args.Target);
        _slots.TryInsert(args.Target, part.ItemInsertionSlot, item, user: null);
    }

    private void OnWoundableShapeshifted(Entity<WoundableComponent> ent, ref ShapeshiftedEvent args)
    {
        _wounds.Clear();
        foreach (var wound in _wound.GetAllWounds(ent, ent.Comp))
        {
            _wounds.Add(wound);
        }

        foreach (var wound in _wounds)
        {
            var severity = wound.Comp.WoundSeverityPoint;
            var group = wound.Comp.DamageGroup;
            _wound.AddWound(ent, wound, severity, group, ent.Comp, wound.Comp);
        }
    }

    #endregion
}
