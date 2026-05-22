// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.SpaceWhale;
using Content.Trauma.Shared.VentCrawling.Components;
using Robust.Client.GameObjects;

namespace Content.Trauma.Client.Xenomorphs.Tail;

public sealed partial class TailVentCrawlSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BeingVentCrawlerComponent, ComponentStartup>(OnStartVentCrawl);
        SubscribeLocalEvent<BeingVentCrawlerComponent, ComponentRemove>(OnStopVentCrawl);
    }

    private void OnStartVentCrawl(Entity<BeingVentCrawlerComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<TailedEntityComponent>(ent, out var tailed))
            return;

        foreach (var segment in tailed.TailSegments)
            _sprite.SetVisible(GetEntity(segment.Segment), false);
    }

    private void OnStopVentCrawl(Entity<BeingVentCrawlerComponent> ent, ref ComponentRemove args)
    {
        if (!TryComp<TailedEntityComponent>(ent, out var tailed))
            return;

        foreach (var segment in tailed.TailSegments)
            _sprite.SetVisible(GetEntity(segment.Segment), true);
    }
}
