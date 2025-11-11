using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.HealthExaminable;
using Content.Shared.IdentityManagement;
using Robust.Shared.Containers;

namespace Content.Trauma.Shared.Body.Organ;

public sealed class OrganExamineSystem : EntitySystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    private EntityQuery<OrganExamineComponent> _query;
    private EntityQuery<BodyPartComponent> _partQuery;

    public override void Initialize()
    {
        base.Initialize();

        _query = GetEntityQuery<OrganExamineComponent>();
        _partQuery = GetEntityQuery<BodyPartComponent>();

        SubscribeLocalEvent<BodyComponent, HealthBeingExaminedEvent>(OnHealthExamined);
    }

    private void OnHealthExamined(Entity<BodyComponent> ent, ref HealthBeingExaminedEvent args)
    {
        EntityUid? identity = null;
        foreach (var (id, organ) in _body.GetBodyOrgans(ent, ent.Comp))
        {
            if (!_query.TryComp(id, out var comp))
                continue;

            if (!GetOrganPart((id, organ), out var part))
                Log.Warning($"Organ {ToPrettyString(id)} of {ToPrettyString(ent)} was not inside a valid bodypart!");

            // cache the identity incase there's multiple OrganExamine organs
            // not done at the start of the function incase there's none to use it
            identity ??= Identity.Entity(ent.Owner, EntityManager);
            args.Message.PushMarkup(Loc.GetString(comp.Examine, ("target", identity), ("part", part)));
        }
    }

    private bool GetOrganPart(Entity<OrganComponent> organ, out BodyPartType partType)
    {
        // mfw this isnt a thing in body system
        partType = BodyPartType.Other;
        if (!_container.TryGetContainingContainer(organ.Owner, out var container) ||
            container.ID != SharedBodySystem.GetOrganContainerId(organ.Comp.SlotId) ||
            !_partQuery.TryComp(container.Owner, out var part))
            return false;

        partType = part.PartType;
        return true;
    }
}
