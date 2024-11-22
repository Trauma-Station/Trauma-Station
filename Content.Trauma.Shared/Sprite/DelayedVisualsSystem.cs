// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Sprite;

public sealed partial class DelayedVisualsSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DelayedVisualsComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<DelayedVisualsComponent, ComponentShutdown>(OnShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DelayedVisualsComponent>();
        var now = _timing.CurTime;
        while (query.MoveNext(out var uid, out var comp))
        {
            if (now < comp.Finished)
                continue;

            RemCompDeferred(uid, comp);
        }
    }

    private void OnMapInit(Entity<DelayedVisualsComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.Finished = _timing.CurTime + ent.Comp.Delay;
        Dirty(ent);
        _appearance.SetData(ent.Owner, ent.Comp.Key, true);
    }

    private void OnShutdown(Entity<DelayedVisualsComponent> ent, ref ComponentShutdown args)
    {
        _appearance.SetData(ent.Owner, ent.Comp.Key, false);
    }
}
