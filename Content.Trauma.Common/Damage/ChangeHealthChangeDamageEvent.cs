// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;

namespace Content.Trauma.Common.Damage;

/// <summary>
/// Raised on an entity taking damage from the HealthChangeEntityEffect system.
/// </summary>
[ByRefEvent]
public record struct OnHealthChangeEvent(DamageSpecifier Damage);
