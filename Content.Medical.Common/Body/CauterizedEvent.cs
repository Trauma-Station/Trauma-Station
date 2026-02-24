// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.FixedPoint;

namespace Content.Medical.Common.Body;

/// <summary>
/// Event raised on a mob after bleeding is reduced.
/// Amount is a positive number of bleed stacks.
/// </summary>
[ByRefEvent]
public record struct CauterizedEvent(FixedPoint2 Amount);
