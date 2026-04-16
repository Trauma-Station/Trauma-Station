// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Procedural.Distance;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Player;
using Content.Trauma.Shared.Kudzu;

namespace Content.Trauma.Client.Kudzu;


public sealed class FoliageVisionSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    private bool _enabled;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FoliageIgnoringVisionComponent, LocalPlayerAttachedEvent>((_, _, args) => OnLocalPlayerAttached(args));
        SubscribeLocalEvent<FoliageIgnoringVisionComponent, LocalPlayerDetachedEvent>((_, _, args) => OnLocalPlayerDetached(args));
        SubscribeLocalEvent<FoliageIgnoringVisionComponent, ComponentStartup>((_, _, args) => OnComponentStartup(args));
        SubscribeLocalEvent<FoliageIgnoringVisionComponent, ComponentShutdown>((_, _, args) => OnComponentShutdown(args));
        SubscribeLocalEvent<IsFoliageComponent, AppearanceChangeEvent>(OnAppearanceChanged);
    }

    private void OnAppearanceChanged(Entity<IsFoliageComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        UpdateFoliageDrawDepth(ent, args.Sprite);
    }

    private void OnLocalPlayerAttached(LocalPlayerAttachedEvent args)
    {
        UpdatePlayerFoliageIgnoringVision();
    }

    private void OnLocalPlayerDetached(LocalPlayerDetachedEvent args)
    {
        UpdatePlayerFoliageIgnoringVision();
    }

    private void OnComponentStartup(ComponentStartup args)
    {
        UpdatePlayerFoliageIgnoringVision();
    }

    private void OnComponentShutdown(ComponentShutdown args)
    {
        UpdatePlayerFoliageIgnoringVision();
    }

    private void RefreshEveryPieceOfFoliage()
    {
        var query = EntityQueryEnumerator<IsFoliageComponent, SpriteComponent>();

        while (query.MoveNext(out var entityUid, out _, out var sprite))
        {
            UpdateFoliageDrawDepth(entityUid, sprite);
        }
    }

    private void UpdateFoliageDrawDepth(EntityUid uid, SpriteComponent? sprite = null)
    {
        if (!Resolve(uid, ref sprite, false))
            return;

        var drawDepth = _enabled
            ? DrawDepth.Default -5
            : DrawDepth.Default +10;
        _sprite.SetDrawDepth((uid, sprite), drawDepth);
    }

    private void UpdatePlayerFoliageIgnoringVision()
    {
        var previousEnabled = _enabled;
        var attached = _player.LocalEntity;
        var shouldHaveFoliageIgnoringVision = attached != null && HasComp<FoliageIgnoringVisionComponent>(attached.Value);
        _enabled = shouldHaveFoliageIgnoringVision;
        if (previousEnabled != _enabled)
            RefreshEveryPieceOfFoliage();
    }
}
