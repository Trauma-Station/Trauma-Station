// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Trauma.Common.Chemistry;

namespace Content.Trauma.Shared.Chemistry;

/// <summary>
/// Adds a comp to everything made with <see cref="RehydratableSystem"/>
/// </summary>
public sealed partial class RehydratedCompSystem : EntitySystem
{

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GotRehydratedEvent>(OnRehydrated);
    }

    private void OnRehydrated(ref GotRehydratedEvent args)
    {
        // differentiates beings of the cube
        EnsureComp<CubeBornComponent>(args.Target);
    }
}
