// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Client.Viewcone.Overlays;
using Content.Trauma.Shared.Viewcone;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Physics;
using Robust.Shared.Player;

namespace Content.Trauma.Client.Viewcone;

/// <summary>
/// Handles adding and removing the viewcone overlays, as well as ferrying data between them
/// </summary>
public sealed class ViewconeOverlaySystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    private ViewconeConeOverlay _coneOverlay = default!;
    private ViewconeSetAlphaOverlay _setAlphaOverlay = default!;
    private ViewconeResetAlphaOverlay _resetAlphaOverlay = default!;

    // slightly balls state management, but
    // done so we don't have to requery within the same frame
    // this is always cleared at the end of resetting alpha
    // it is the least thread safe code of all time obviously. but rendering not threaded. so
    // we can abuse the fact that the overlays will always draw sequentially in the order we expect, and
    // one wont start rendering in the middle of rendering another
    internal List<(Entity<SpriteComponent> ent, float baseAlpha)> CachedBaseAlphas = new(128);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ViewconeComponent, ComponentInit>(OnConeManInit);
        SubscribeLocalEvent<ViewconeComponent, ComponentShutdown>(OnConeManShutdown);

        SubscribeLocalEvent<ViewconeComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<ViewconeComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        _coneOverlay = new();
        _setAlphaOverlay = new();
        _resetAlphaOverlay = new();
    }

    private void OnPlayerAttached(Entity<ViewconeComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        AddOverlays();
    }

    private void OnPlayerDetached(Entity<ViewconeComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        RemoveOverlays();
    }

    // TODO: this wont work if you have a non-viewcone entity looking at a viewcone camera or something?
    private void OnConeManInit(Entity<ViewconeComponent> ent, ref ComponentInit args)
    {
        if (ent.Owner == _player.LocalEntity)
            AddOverlays();
    }

    private void OnConeManShutdown(Entity<ViewconeComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Owner == _player.LocalEntity)
            RemoveOverlays();
    }

    private void AddOverlays()
    {
        _overlay.AddOverlay(_coneOverlay);
        _overlay.AddOverlay(_setAlphaOverlay);
        _overlay.AddOverlay(_resetAlphaOverlay);
    }

    private void RemoveOverlays()
    {
        _overlay.RemoveOverlay(_coneOverlay);
        _overlay.RemoveOverlay(_setAlphaOverlay);
        _overlay.RemoveOverlay(_resetAlphaOverlay);
    }
}
