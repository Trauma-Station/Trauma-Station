// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Atmos.Rotting;
using Content.Shared.Body;

namespace Content.Medical.Shared.Body;

/// <summary>
/// Prevents perishable organs/bodyparts from rotting inside a living mob.
/// </summary>
public sealed partial class OrganRottingSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OrganComponent, IsRottingEvent>(OnIsRotting);
    }

    private void OnIsRotting(Entity<OrganComponent> ent, ref IsRottingEvent args)
    {
        args.Handled |= ent.Comp.Body == null;
    }
}
