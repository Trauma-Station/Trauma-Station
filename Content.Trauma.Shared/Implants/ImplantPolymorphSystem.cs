// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Polymorph;
using Content.Shared.Whitelist;

/// <summary>
/// Tries to transfer implants to the new entity when the old implanted one is polymorphed.
/// </summary>
public sealed partial class ImplantPolymorphSystem : EntitySystem
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedSubdermalImplantSystem _implant = default!;
    [Dependency] private EntityQuery<SubdermalImplantComponent> _query = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ImplantedComponent, PolymorphedEvent>(OnPolymorphed);
    }

    private void OnPolymorphed(Entity<ImplantedComponent> ent, ref PolymorphedEvent args)
    {
        // copy it to prevent collection modification error
        var implants = new List<EntityUid>(ent.Comp.ImplantContainer.ContainedEntities);
        var target = args.NewEntity;
        foreach (var implant in implants)
        {
            if (!_query.TryComp(implant, out var comp) ||
                !_whitelist.CheckBoth(target, comp.Blacklist, comp.Whitelist))
                continue;

            _implant.ForceImplant(target, (implant, comp));
        }
    }
}
