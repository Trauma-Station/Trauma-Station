// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Projectiles;

/// <summary>
/// Event raised on a cartridge when it is being shot and spawns its bullet entity.
/// </summary>
[ByRefEvent]
public record struct CartridgeFiredEvent(EntityUid Bullet);
