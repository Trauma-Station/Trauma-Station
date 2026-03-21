// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Kudzu;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Trauma.Client.Kudzu;

public sealed class SeeOverObstacleSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SeeOverObstacleComponent, ComponentStartup>(OnObstacleStartup);
        SubscribeLocalEvent<SeeOverObstacleComponent, AppearanceChangeEvent>(OnObstacleAppearanceChange);
        SubscribeLocalEvent<SeeOverObstacleComponent, RefreshSeeOverObstacleVisualsEvent>(OnObstacleRefresh);

        SubscribeLocalEvent<SeeOverObstaclesComponent, ComponentStartup>(OnViewerStartup);
        SubscribeLocalEvent<SeeOverObstaclesComponent, ComponentShutdown>(OnViewerShutdown);

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnLocalPlayerAttached);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnLocalPlayerDetached);
    }

    private void OnObstacleStartup(Entity<SeeOverObstacleComponent> ent, ref ComponentStartup args)
    {
        UpdateObstacleDrawDepth(ent);
    }

    private void OnObstacleAppearanceChange(Entity<SeeOverObstacleComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        UpdateObstacleDrawDepth(ent, args.Sprite);
    }

    private void OnObstacleRefresh(Entity<SeeOverObstacleComponent> ent, ref RefreshSeeOverObstacleVisualsEvent args)
    {
        UpdateObstacleDrawDepth(ent);
    }

    private void OnViewerStartup(Entity<SeeOverObstaclesComponent> ent, ref ComponentStartup args)
    {
        if (_player.LocalSession?.AttachedEntity == ent.Owner)
            RefreshAllObstacles();
    }

    private void OnViewerShutdown(Entity<SeeOverObstaclesComponent> ent, ref ComponentShutdown args)
    {
        if (_player.LocalSession?.AttachedEntity == ent.Owner)
            RefreshAllObstacles();
    }

    private void OnLocalPlayerAttached(LocalPlayerAttachedEvent args)
    {
        RefreshAllObstacles();
    }

    private void OnLocalPlayerDetached(LocalPlayerDetachedEvent args)
    {
        RefreshAllObstacles();
    }

    private void RefreshAllObstacles()
    {
        var query = EntityQueryEnumerator<SeeOverObstacleComponent>();

        while (query.MoveNext(out var uid, out _))
        {
            var ev = new RefreshSeeOverObstacleVisualsEvent();
            RaiseLocalEvent(uid, ref ev);
        }
    }

    private void UpdateObstacleDrawDepth(Entity<SeeOverObstacleComponent> ent, SpriteComponent? sprite = null)
    {
        if (!Resolve(ent.Owner, ref sprite, false))
            return;

        var drawDepth = LocalPlayerCanSeeOverObstacles()
            ? ent.Comp.SeeOverDrawDepth
            : ent.Comp.NormalDrawDepth;

        _sprite.SetDrawDepth((ent.Owner, sprite), drawDepth);
    }

    private bool LocalPlayerCanSeeOverObstacles()
    {
        var attached = _player.LocalSession?.AttachedEntity;
        return attached != null && HasComp<SeeOverObstaclesComponent>(attached.Value);
    }
}
