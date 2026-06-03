// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Sprite;
using Content.Trauma.Shared.VentCrawling.Components;

namespace Content.Trauma.Client.Xenomorphs.Tail;

public sealed partial class TailVentCrawlSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BeingVentCrawlerComponent, ComponentStartup>(OnStartVentCrawl);
        SubscribeLocalEvent<BeingVentCrawlerComponent, ComponentRemove>(OnStopVentCrawl);
    }

    private void OnStartVentCrawl(Entity<BeingVentCrawlerComponent> ent, ref ComponentStartup args)
    {
        var ev = new UpdateSpriteVisibilityEvent(nameof(BeingVentCrawlerComponent), 0f);
        RaiseLocalEvent(ent, ref ev);
    }

    private void OnStopVentCrawl(Entity<BeingVentCrawlerComponent> ent, ref ComponentRemove args)
    {
        var ev = new UpdateSpriteVisibilityEvent(nameof(BeingVentCrawlerComponent), 1f);
        RaiseLocalEvent(ent, ref ev);
    }
}
