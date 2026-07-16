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

        SubscribeLocalEvent<BeingVentCrawlerComponent, ComponentStartup>((x, _, _) => UpdateTailVisibility(x, 0f));
        SubscribeLocalEvent<BeingVentCrawlerComponent, ComponentShutdown>((x, _, _) => UpdateTailVisibility(x, 1f));
        SubscribeLocalEvent<TailedEntityComponent, ComponentStartup>(OnTailedStartup);
    }

    private void OnTailedStartup(Entity<TailedEntityComponent> ent, ref ComponentStartup args)
    {
        if (!HasComp<BeingVentCrawlerComponent>(ent))
            return;

        UpdateTailVisibility(ent.AsNullable(), 0f);
    }

    private void UpdateTailVisibility(Entity<TailedEntityComponent?> ent, float alpha)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        foreach (var data in ent.Comp.TailSegments)
        {
            if (!TryGetEntity(data.Segment, out var uid))
                continue;

            _spriteVis.UpdateVisibilityModifiers(uid.Value, nameof(BeingVentCrawlerComponent), alpha);
        }
    }
}
