// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Trauma.Common.Heretic;

[Serializable, NetSerializable]
public sealed class ButtonTagPressedEvent(string id, NetEntity user, NetCoordinates coords) : EntityEventArgs
{
    public NetEntity User = user;

    public NetCoordinates Coords = coords;

    public string Id = id;
}

[ByRefEvent]
public record struct HereticCheckEvent(EntityUid Uid, bool Result = false);

[ByRefEvent]
public record struct ParentPacketReceiveAttemptEvent(bool Cancelled = false);

[ByRefEvent]
public record struct GetVirtualItemBlockingEntityEvent(EntityUid Uid);

[ByRefEvent]
public record struct BeforeAccessReaderCheckEvent(bool Cancelled = false);

[ByRefEvent]
public record struct BeforeHolosignUsedEvent(EntityUid User, EntityCoordinates ClickLocation, bool Handled = false, bool Cancelled = false);

[ByRefEvent]
public readonly record struct IconSmoothCornersInitializedEvent;
