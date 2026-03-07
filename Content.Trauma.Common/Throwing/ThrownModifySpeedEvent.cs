// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Throwing;

/// <summary>
/// Raised on an entity whose speed is about to be modified by something throwing it.
/// </summary>
[ByRefEvent]
public record struct ModifyThrownSpeedEvent(EntityUid User, float BaseThrowSpeed, float Distance);
