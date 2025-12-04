// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.Containers;

namespace Content.Trauma.Common.Body.Part;

/// <summary>
/// Raised on a body part to find its cavity container, for storing an item via surgery.
/// </summary>
[ByRefEvent]
public record struct GetBodyPartCavityEvent(ContainerSlot? Container = null);
