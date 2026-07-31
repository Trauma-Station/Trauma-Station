// SPDX-License-Identifier: AGPL-3.0-or-later


using Content.Trauma.Shared.Waypointer.Components;

namespace Content.Trauma.Shared.Heretic.Components;

/// <summary>
/// "Clientside" version of <see cref="ActiveWaypointerComponent"/>
/// Server doesn't change this in any way but waypointer overlay still process it
/// Useful when you don't need any actions or pvs overrides
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SimpleWaypointerComponent : ActiveWaypointerComponent;
