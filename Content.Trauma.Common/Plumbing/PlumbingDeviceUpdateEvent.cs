// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Plumbing;

/// <summary>
/// Raised directed on an plumbing device as part of the plumbing update loop when the device should do processing.
/// Use this for plumbing devices instead of <see cref="EntitySystem.Update"/>.
/// </summary>
[ByRefEvent]
public record struct PlumbingDeviceUpdateEvent(float FrameTime);
