// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Item;
using Content.Trauma.Common.Weapons.Ranged;

namespace Content.Trauma.Shared.Weapons.Ranged;

public sealed partial class BasicAmmoHeldVisualsSystem : EntitySystem
{
    [Dependency] SharedItemSystem _item = default!;

    [SubscribeLocalEvent]
    private void OnAmmoCountChanged(Entity<BasicAmmoHeldVisualsComponent> ent, ref BasicAmmoChangedEvent args)
    {
        var loaded = args.Count != 0;
        var prefix = loaded ? ent.Comp.LoadedPrefix : ent.Comp.EmptyPrefix;
        _item.SetHeldPrefix(ent.Owner, prefix);
    }
}
