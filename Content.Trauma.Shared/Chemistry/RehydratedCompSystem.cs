// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;

namespace Content.Trauma.Shared.Chemistry;

/// <summary>
/// Adds a comp to everything made with <see cref="RehydratableSystem"/>
/// </summary>
public sealed partial class RehydratedCompSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnRehydrated(Entity<RehydratableComponent> ent, ref GotRehydratedEvent args)
    {
        // differentiates beings of the cube
        EnsureComp<CubeBornComponent>(args.Target);
    }
}
