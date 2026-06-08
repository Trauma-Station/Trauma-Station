// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.SpaceWhale;
using Content.Trauma.Common.Sprite;
using Content.Trauma.Shared.VentCrawling.Components;

namespace Content.Trauma.Client.Xenomorphs.Tail;

public sealed partial class TailVentCrawlSystem : EntitySystem
{
    [Dependency] private CommonSpriteVisibilitySystem _spriteVis = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BeingVentCrawlerComponent, ComponentStartup>(OnStartVentCrawl);
        SubscribeLocalEvent<BeingVentCrawlerComponent, ComponentRemove>(OnStopVentCrawl);
    }

    private void OnStartVentCrawl(Entity<BeingVentCrawlerComponent> ent, ref ComponentStartup args)
    {
        UpdateTailVisibility(ent, 0f);
    }

    private void OnStopVentCrawl(Entity<BeingVentCrawlerComponent> ent, ref ComponentRemove args)
    {
        UpdateTailVisibility(ent, 1f);
    }

    private void UpdateTailVisibility(EntityUid uid, float alpha)
    {
        if (!TryComp(uid, out TailedEntityComponent? comp))
            return;

        foreach (var data in comp.TailSegments)
        {
            if (!TryGetEntity(data.Segment, out var ent))
                continue;

            _spriteVis.UpdateVisibilityModifiers(ent.Value, nameof(BeingVentCrawlerComponent), alpha);
        }
    }
}
