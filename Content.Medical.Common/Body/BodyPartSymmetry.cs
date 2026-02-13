// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.Serialization;

namespace Content.Medical.Common.Body;

/// <summary>
/// Defines the symmetry of a body part.
/// </summary>
[Serializable, NetSerializable]
public enum BodyPartSymmetry : byte
{
    None = 0,
    Left,
    Right
}
