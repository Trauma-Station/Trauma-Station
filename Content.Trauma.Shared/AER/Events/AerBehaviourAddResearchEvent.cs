// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Shared.AER;

/// <summary>
/// event raised for giving research on an aer behaviour
/// </summary>
[ByRefEvent]
public record struct AerBehaviourAddResearchEvent(EntityUid Aer);
