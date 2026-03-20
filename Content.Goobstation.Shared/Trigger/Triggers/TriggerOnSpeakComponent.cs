// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Trigger.Components.Triggers;
using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.Trigger.Triggers;

/// <summary>
/// Triggers when the parent entity, or this one, speaks.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnSpeakComponent : BaseTriggerOnXComponent
{
    /// <summary>
    /// The range at which it listens for speech.
    /// </summary>
    [DataField]
    public int ListenRange = 4;
}
