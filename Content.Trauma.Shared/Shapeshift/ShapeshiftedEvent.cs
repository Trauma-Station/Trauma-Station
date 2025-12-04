// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Body.Components;

namespace Content.Trauma.Shared.Shapeshift;

/// <summary>
/// Raised on the original organ or bodypart when it is being shapeshifted into a new one.
/// Not raised if the organ/part is just being reattached (cybernetics for example).
/// The body and target are the new body and target organ/bodypart respectively.
/// </summary>
[ByRefEvent]
public readonly record struct ShapeshiftedEvent(Entity<BodyComponent> Body, EntityUid Target);
