// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions.Components;
using Content.Trauma.Shared.Heretic.Components;
using Content.Trauma.Shared.Waypointer;
using Content.Trauma.Shared.Waypointer.Components;
using Content.Trauma.Shared.Waypointer.Events;
using Robust.Client.Player;
using Robust.Client.Timing;
using Robust.Shared.Player;

namespace Content.Trauma.Client.Waypointer;

/// <summary>
/// The client-side system handles initializing the overlay, as well as removing and adding it depending on game actions.
/// </summary>
public sealed partial class WaypointerSystem : SharedWaypointerSystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private IClientGameTiming _timing = default!;

    private WaypointerOverlay _waypointerOverlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActiveWaypointerComponent, ComponentStartup>(OnAddition);
        SubscribeLocalEvent<ActiveWaypointerComponent, ComponentShutdown>(OnRemoval);

        SubscribeLocalEvent<ActiveWaypointerComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<ActiveWaypointerComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<SimpleWaypointerComponent, ComponentStartup>(OnAddition);
        SubscribeLocalEvent<SimpleWaypointerComponent, ComponentShutdown>(OnRemoval);

        SubscribeLocalEvent<SimpleWaypointerComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<SimpleWaypointerComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        _waypointerOverlay = new WaypointerOverlay();
    }

    private void OnAddition(EntityUid player, ActiveWaypointerComponent comp, ref ComponentStartup args)
    {
        if (_player.LocalEntity != player)
            return;

        _overlay.AddOverlay(_waypointerOverlay);
    }

    private void OnRemoval(EntityUid player, ActiveWaypointerComponent comp, ref ComponentShutdown args)
    {
        if (_player.LocalEntity != player)
            return;

        _overlay.RemoveOverlay(_waypointerOverlay);
    }

    protected override void OnWaypointersToggled(Entity<ActionComponent> action, ref WaypointersToggledMessage args)
    {
        base.OnWaypointersToggled(action, ref args);

        if (args.IsActive)
            _overlay.AddOverlay(_waypointerOverlay);
        else
            _overlay.RemoveOverlay(_waypointerOverlay);
    }

    private void OnPlayerAttached(EntityUid player, ActiveWaypointerComponent comp, LocalPlayerAttachedEvent args)
    {
        _overlay.AddOverlay(_waypointerOverlay);
    }

    private void OnPlayerDetached(EntityUid player, ActiveWaypointerComponent comp, LocalPlayerDetachedEvent args)
    {
        _overlay.RemoveOverlay(_waypointerOverlay);
    }
}
