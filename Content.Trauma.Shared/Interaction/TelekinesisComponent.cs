// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Actions;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Interaction;

/// <summary>
/// Lets you ignore action blockers while conscious and interact with obstructed entities, if they are still in range.
/// </summary>
/// <remarks>
/// Not relayed to mutations and handled there because interaction is really, really common.
/// </remarks>
[RegisterComponent, NetworkedComponent]
public sealed partial class TelekinesisComponent : Component;

/// <summary>
/// Event for tethering the target entity.
/// </summary>
public sealed partial class TelekinesisActionEvent : EntityTargetActionEvent;
