// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Magic;
using Content.Trauma.Shared.Heretic.Components.Ghoul;

namespace Content.Trauma.Shared.Heretic.Systems;

public abstract class SharedGhoulSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GhoulComponent, BeforeMindSwappedEvent>(OnBeforeMindSwap);
    }

    private void OnBeforeMindSwap(Entity<GhoulComponent> ent, ref BeforeMindSwappedEvent args)
    {
        args.Cancelled = true;
        args.Message = "ghoul";
    }
}
