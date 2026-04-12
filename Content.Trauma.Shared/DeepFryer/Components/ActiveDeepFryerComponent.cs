// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Shared.DeepFryer.Components;

/// <summary>
/// Component added to deep fryers that have power and are closed.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActiveDeepFryerComponent : Component;
// TODO: timespans for updating not frametime slop
