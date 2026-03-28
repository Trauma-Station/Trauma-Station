// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Shitcode.Common.Damage;

/// <summary>
/// Ignore any damage taken from the HealthChangeEntityEffect system. Raised on an entity.
/// </summary>
[Serializable, NetSerializable]
public record struct IgnoreHeathChangeEvent(bool Immune);
