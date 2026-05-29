// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Polymorph;

/// <summary>
/// Raised on an entity when it is used as the template for a chameleon disguise.
/// </summary>
[ByRefEvent]
public record struct ChameleonDisguisedEvent(EntityUid Disguise);
