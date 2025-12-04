// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Trigger;

/// <summary>
/// Starts the timer trigger on map init.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AutoStartTimerComponent : Component;
