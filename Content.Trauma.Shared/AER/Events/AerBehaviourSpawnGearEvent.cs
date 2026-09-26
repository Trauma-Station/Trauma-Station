// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.AER;

/// <summary>
/// event raised for spawning an aer I.D. gear on an aer behaviour
/// </summary>
[ByRefEvent]
public record struct AerBehaviourSpawnGearEvent(EntityUid Aer);
