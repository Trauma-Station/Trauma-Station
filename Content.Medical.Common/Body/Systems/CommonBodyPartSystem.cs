// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Medical.Common.Targeting;

namespace Content.Medical.Common.Body;

/// <summary>
/// Provides API for any module to do basic operations on bodyparts.
/// </summary>
public abstract class CommonBodyPartSystem : EntitySystem
{
    /// <summary>
    /// Get a list of every body part matching a given type and symmetry.
    /// </summary>
    public abstract List<EntityUid> GetBodyParts(EntityUid body, BodyPartType? partType, BodyPartSymmetry? symmetry = null);

    /// <summary>
    /// Tries to add an organ slot to this bodypart.
    /// The slot can be for an internal or external organ.
    /// </summary>
    /// <returns>true if the part is valid and didn't already have the slot</returns>
    public abstract bool TryAddSlot(EntityUid organ, [ForbidLiteral] string category);

    /// <summary>
    /// Gets the part of a body part, or null if it isn't one.
    /// </summary>
    public abstract BodyPartType? GetPartType(EntityUid uid);

    /// <summary>
    /// Gets the symmetry of a body part, defaulting to none.
    /// </summary>
    public abstract BodyPartSymmetry GetSymmetry(EntityUid uid);

    /// <summary>
    /// Gets the targeting part enum for a body part, or null if it has none.
    /// </summary>
    public abstract TargetBodyPart? GetTargetBodyPart(EntityUid uid);
}
