using Content.Medical.Shared.Wounds;
using Content.Medical.Shared.Traumas;
using Content.Shared.Body;
using Content.Shared.Damage.Systems;
using Content.Shared.Rejuvenate;

namespace Content.Medical.Shared.Body;

public sealed class BodyRestoreSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _body = default!;
    //[Dependency] private readonly WoundSystem _wound = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, RejuvenateEvent>(OnRejuvenate,
            before: [ typeof(DamageableSystem) ]);
    }

    private void OnRejuvenate(Entity<BodyComponent> ent, ref RejuvenateEvent args)
    {
        RestoreBody(ent.AsNullable());
        // not using RelayEvent because it wraps it in BodyRelayedEvent
        foreach (var organ in _body.GetOrgans(ent.AsNullable()))
        {
            RaiseLocalEvent(organ, args); // TODO: make by ref if it stops being a class
        }
    }

    // jesus christ
    public void RestoreBody(Entity<BodyComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return;

        var ent = entity.Owner;
        var body = entity.Comp;

        if (Prototype(entity) is not {} proto)
            return;
        /* TODO NUBODY: fuck, make it use InitialBody?

        var prototype = Prototypes.Index(body.Prototype.Value);

        if (!TryGetRootPart(ent, out var rootPart))
            return;

        var rootSlot = prototype.Root;
        foreach (var organ in prototype.Slots[rootSlot].Organs)
        {
            if (!Containers.TryGetContainer(rootPart.Value.Owner, GetOrganContainerId(organ.Key), out var organContainer))
                continue;

            var organEnt = organContainer.ContainedEntities.FirstOrNull();
            if (organEnt != null)
            {
                foreach (var modifier in Comp<OrganComponent>(organEnt.Value).IntegrityModifiers)
                {
                    _trauma.TryRemoveOrganDamageModifier(organEnt.Value, modifier.Key.Item2, modifier.Key.Item1);
                }
            }
            else
            {
                SpawnInContainerOrDrop(organ.Value, rootPart.Value.Owner, GetOrganContainerId(organ.Key));
            }
        }

        Dirty(rootPart.Value.Owner, rootPart.Value.Comp);

        var frontier = new Queue<string>();
        frontier.Enqueue(rootSlot);

        var cameFrom = new Dictionary<string, string>();
        cameFrom[rootSlot] = rootSlot;

        var cameFromEntities = new Dictionary<string, EntityUid>();
        cameFromEntities[rootSlot] = rootPart.Value.Owner;

        while (frontier.TryDequeue(out var currentSlotId))
        {
            var currentSlot = prototype.Slots[currentSlotId];

            foreach (var connection in currentSlot.Connections)
            {
                if (!cameFrom.TryAdd(connection, currentSlotId))
                    continue;

                var connectionSlot = prototype.Slots[connection];
                var parentEntity = cameFromEntities[currentSlotId];
                var parentPartComponent = Comp<BodyPartComponent>(parentEntity);

                if (Containers.TryGetContainer(parentEntity, GetPartSlotContainerId(connection), out var container))
                {
                    if (container.ContainedEntities.Count > 0)
                    {
                        var containedEnt = container.ContainedEntities[0];
                        var containedPartComp = Comp<BodyPartComponent>(containedEnt);
                        cameFromEntities[connection] = containedEnt;

                        foreach (var organ in connectionSlot.Organs)
                        {
                            if (Containers.TryGetContainer(containedEnt, GetOrganContainerId(organ.Key), out var organContainer))
                            {
                                var organEnt = organContainer.ContainedEntities.FirstOrNull();
                                if (organEnt != null)
                                {
                                    foreach (var modifier in Comp<OrganComponent>(organEnt.Value).IntegrityModifiers)
                                    {
                                        _trauma.TryRemoveOrganDamageModifier(organEnt.Value, modifier.Key.Item2, modifier.Key.Item1);
                                    }
                                }
                                else
                                {
                                    SpawnInContainerOrDrop(organ.Value, containedEnt, GetOrganContainerId(organ.Key));
                                }
                            }
                            else
                            {
                                var slot = CreateOrganSlot((containedEnt, containedPartComp), organ.Key);
                                SpawnInContainerOrDrop(organ.Value, containedEnt, GetOrganContainerId(organ.Key));

                                if (slot is null)
                                {
                                    Log.Error($"Could not create organ for slot {organ.Key} in {ToPrettyString(ent)}");
                                }
                            }
                        }
                    }
                    else
                    {
                        var childPart = Spawn(connectionSlot.Part, new EntityCoordinates(parentEntity, Vector2.Zero));
                        cameFromEntities[connection] = childPart;

                        var childPartComponent = Comp<BodyPartComponent>(childPart);

                        var partSlot = new BodyPartSlot(connection, childPartComponent.PartType, childPartComponent.Symmetry);
                        childPartComponent.ParentSlot = partSlot;
                        parentPartComponent.Children.TryAdd(connection, partSlot);

                        Dirty(parentEntity, parentPartComponent);
                        Dirty(childPart, childPartComponent);

                        Containers.Insert(childPart, container);

                        SetupOrgans((childPart, childPartComponent), connectionSlot.Organs);
                    }
                }
                else
                {
                    var childPart = Spawn(connectionSlot.Part, new EntityCoordinates(parentEntity, Vector2.Zero));
                    cameFromEntities[connection] = childPart;

                    var childPartComponent = Comp<BodyPartComponent>(childPart);

                    var partSlot = CreatePartSlot(parentEntity, connection, childPartComponent.PartType, childPartComponent.Symmetry, parentPartComponent);
                    childPartComponent.ParentSlot = partSlot;

                    Dirty(parentEntity, parentPartComponent);
                    Dirty(childPart, childPartComponent);

                    if (partSlot is null)
                    {
                        Log.Error($"Could not create slot for connection {connection} in body {prototype.ID}");
                        QueueDel(childPart);
                        continue;
                    }

                    container = Containers.GetContainer(parentEntity, GetPartSlotContainerId(connection));
                    Containers.Insert(childPart, container);

                    SetupOrgans((childPart, childPartComponent), connectionSlot.Organs);
                }

                frontier.Enqueue(connection);
            }
        }


        if (_trauma.TryGetBodyTraumas(ent, out var traumas, bodyComp: body))
            foreach (var trauma in traumas)
                _trauma.RemoveTrauma(trauma);

        foreach (var bodyPart in GetBodyChildren(ent, body))
        {
            if (!TryComp<WoundableComponent>(bodyPart.Id, out var woundable))
                continue;

            var bone = woundable.Bone.ContainedEntities.FirstOrNull();
            if (TryComp<BoneComponent>(bone, out var boneComp))
                _trauma.SetBoneIntegrity(bone.Value, boneComp.IntegrityCap, boneComp);

            _wound.TryHaltAllBleeding(bodyPart.Id, woundable);
            _wound.ForceHealWoundsOnWoundable(bodyPart.Id, out _);
        }
        */
    }
}
