// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.Knowledge.Components;

/// <summary>
/// Put on a weapon to stop <see cref="MeleeSpeedKnowledgeComponent"/> speeding it up.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NoMeleeSpeedKnowledgeComponent : Component;
