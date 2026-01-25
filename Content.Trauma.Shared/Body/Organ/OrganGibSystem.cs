// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Gibbing;
using Robust.Shared.Containers;

namespace Content.Trauma.Shared.Body.Organ;

/// <summary>
/// Runs logic for organ removal if an organ is gibbed.
/// </summary>
public sealed class OrganGibSystem : EntitySystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OrganComponent, BeingGibbedEvent>(OnGibbed);
    }

    private void OnGibbed(Entity<OrganComponent> ent, ref BeingGibbedEvent args)
    {
        if (!_container.TryGetContainingContainer(ent.Owner, out var container))
            return;

        var parent = container.Owner;
        if (!HasComp<BodyPartComponent>(parent) || TerminatingOrDeleted(parent))
            return;

        _body.RemoveOrgan(ent, parent);
    }
}
