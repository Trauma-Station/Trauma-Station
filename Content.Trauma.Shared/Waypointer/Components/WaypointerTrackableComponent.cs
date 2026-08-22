// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Waypointer.Components;

/// <summary>
/// This is on entities that ARE NOT GRIDS to be trackable by Waypointers.
/// If you want an entity that isn't a grid to be tracked by a waypointer, put this on them.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class WaypointerTrackableComponent : Component;
