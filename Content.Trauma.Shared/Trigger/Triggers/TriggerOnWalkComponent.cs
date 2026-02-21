// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Trigger.Components.Triggers;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Trigger.Triggers;

/// <summary>
/// Triggers every time this entity walks enough to make a footstep sound.
/// Does nothing if you can't make footstep sounds e.g. with ninja shoes.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class TriggerOnWalkComponent : BaseTriggerOnXComponent;
