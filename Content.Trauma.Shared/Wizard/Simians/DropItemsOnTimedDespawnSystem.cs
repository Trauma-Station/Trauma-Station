// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Wizard.FadingTimedDespawn;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Spawners;

namespace Content.Trauma.Shared.Wizard.Simians;

public sealed partial class DropItemsOnTimedDespawnSystem : EntitySystem
{
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private EntityQuery<TimedDespawnComponent> _despawnQuery = default!;
    [Dependency] private EntityQuery<FadingTimedDespawnComponent> _fadingQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DropItemsOnTimedDespawnComponent, TimedDespawnEvent>(OnDespawn);
    }

    private void OnDespawn(Entity<DropItemsOnTimedDespawnComponent> ent, ref TimedDespawnEvent args)
    {
        var (uid, comp) = ent;

        if (!TryComp(uid, out HandsComponent? hands))
            return;

        foreach (var hand in _hands.EnumerateHands((uid, hands)))
        {
            if (_hands.TryGetActiveItem((uid, hands), out var held))
                continue;

            if (!comp.DropDespawningItems && (_fadingQuery.HasComp(held) || _despawnQuery.HasComp(held)))
                continue;

            _hands.TryDrop((uid, hands), hand);
        }
    }
}
