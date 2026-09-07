// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Server.Aer.Objectives;

/// <summary>
/// checks if the entity is connected to a aer sensor and in range (contained)
/// </summary>
[RegisterComponent, Access(typeof(AerShyGuyObjectiveSystem))]
public sealed partial class AerShyGuyObjectiveComponent : Component
{
}
