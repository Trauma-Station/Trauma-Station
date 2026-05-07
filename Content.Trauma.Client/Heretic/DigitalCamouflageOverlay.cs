// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Movement.Components;
using Content.Trauma.Common.Heretic;
using Content.Trauma.Shared.AudioMuffle;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;

namespace Content.Trauma.Client.Heretic;

public sealed class DigitalCamouflageOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.ScreenSpace | OverlaySpace.WorldSpaceBelowEntities;

    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    private readonly SpriteSystem _sprite;

    private readonly HashSet<Entity<SpriteComponent>> _hiddenEntities = new();

    public DigitalCamouflageOverlay()
    {
        IoCManager.InjectDependencies(this);

        _sprite = _entMan.System<SpriteSystem>();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (args.Space == OverlaySpace.ScreenSpace)
            return true;

        if (_player.LocalEntity is not { } player)
            return false;

        if (args.Viewport.Eye != _eye.CurrentEye)
            return true;

        return _entMan.TryGetComponent(player, out RelayInputMoverComponent? relay) &&
               _entMan.HasComponent<AiEyeComponent>(relay.RelayEntity);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.Space == OverlaySpace.ScreenSpace)
        {
            foreach (var ent in _hiddenEntities)
            {
                _sprite.SetVisible(ent.AsNullable(), true);
            }

            _hiddenEntities.Clear();
            return;
        }

        var query = _entMan.EntityQueryEnumerator<SpriteComponent>();
        while (query.MoveNext(out var uid, out var sprite))
        {
            if (!sprite.Visible)
                continue;

            var ev = new CanSeeOnCameraEvent(uid);
            _entMan.EventBus.RaiseLocalEvent(uid, ref ev);
            if (!ev.Cancelled)
                continue;

            _sprite.SetVisible((uid, sprite), false);
            _hiddenEntities.Add((uid, sprite));
        }
    }
}
