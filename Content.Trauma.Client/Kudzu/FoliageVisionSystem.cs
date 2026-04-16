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
        SubscribeLocalEvent<FoliageIgnoringVisionComponent, ComponentStartup>((_, _, args) => OnPlayerComponentStartup(args));
        SubscribeLocalEvent<FoliageIgnoringVisionComponent, ComponentShutdown>((_, _, args) => OnPlayerComponentShutdown(args));
        SubscribeLocalEvent<IsFoliageComponent, ComponentStartup>((uid, _, args) => OnKudzuComponentStartup(uid, args));
        SubscribeLocalEvent<IsFoliageComponent, ComponentShutdown>((uid, _, args) => OnKudzuComponentShutdown(uid, args));
        SubscribeLocalEvent<IsFoliageComponent, AppearanceChangeEvent>(OnAppearanceChanged);
    }

    private void OnAppearanceChanged(Entity<IsFoliageComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        UpdateFoliageDrawDepth(ent, args.Sprite);
    }

    // Attaches detaches
    private void OnLocalPlayerAttached(LocalPlayerAttachedEvent args)
    {
        UpdatePlayerFoliageIgnoringVision();
    }

    private void OnLocalPlayerDetached(LocalPlayerDetachedEvent args)
    {
        UpdatePlayerFoliageIgnoringVision();
    }

    // Player Startup/Shutdown
    private void OnPlayerComponentStartup(ComponentStartup args)
    {
        UpdatePlayerFoliageIgnoringVision();
    }

    private void OnPlayerComponentShutdown(ComponentShutdown args)
    {
        UpdatePlayerFoliageIgnoringVision();
    }

    // Kudzu Startup/Shutdown
    private void OnKudzuComponentStartup(EntityUid uid, ComponentStartup args)
    {
        UpdateFoliageDrawDepth(uid);
    }

    private void OnKudzuComponentShutdown(EntityUid uid, ComponentShutdown args)
    {
        UpdateFoliageDrawDepth(uid);
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
