// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Player;
using Content.Trauma.Shared.Kudzu;

namespace Content.Trauma.Client.Kudzu;


public sealed class FoliageVisionSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;
    [DataField] private bool _playerHasFoliageIgnoringVision;

    public override void Initialize()
    {
        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnLocalPlayerAttached);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnLocalPlayerDetached);

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

    private void UpdateFoliageDrawDepth(Entity<IsFoliageComponent> ent, SpriteComponent? sprite = null)
    {
        if (!Resolve(ent.Owner, ref sprite, false))
            return;

        var drawDepth = _playerHasFoliageIgnoringVision
            ? Content.Shared.DrawDepth.DrawDepth.FloorObjects.CompareTo(DrawDepth.Default) // Help! This probably shouldn't be hardcoded!
            : sprite.DrawDepth;
        _spriteSystem.SetDrawDepth((ent.Owner, sprite), drawDepth);
    }

    private void UpdatePlayerFoliageIgnoringVision()
    {
        var attached = _playerManager.LocalEntity;
        var shouldHaveFoliageIgnoringVision = attached != null && HasComp<FoliageIgnoringVisionComponent>(attached.Value);
        _playerHasFoliageIgnoringVision = shouldHaveFoliageIgnoringVision;
    }
}
