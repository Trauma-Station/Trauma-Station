// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Robust.Shared.Map;

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

[ByRefEvent]
public record struct ValidateInstantWorldTargetActionEvent(EntityUid User, EntityUid Provider, bool Result = false);

[ByRefEvent]
public readonly record struct TryPerformInstantWorldTargetActionEvent;

[ByRefEvent]
public readonly record struct ConsumingFoodEvent(EntityUid Food, FixedPoint2 Volume);

[ByRefEvent]
public record struct BeforeSpawnPullingVirtualItemsEvent(EntityUid Puller, EntityUid Pulled, bool Cancelled = false);

[ByRefEvent]
public record struct GetGrabMovespeedEvent(float Speed);

[ByRefEvent]
public record struct CanStandWhileImmobileEvent(bool CanStand = false);

[ByRefEvent]
public record struct BeforeMovespeedModifierAppliedEvent(float WalkModifier, float SprintModifier);

[ByRefEvent]
public record struct GetExamineRangeEvent(float Range);

[ByRefEvent]
public record struct ShouldBlockContextMenuEvent(EntityUid Target, bool ShouldBlock = false);

[ByRefEvent]
public record struct GetFirestackPassiveModifierEvent(bool OnFire, bool Resisting, float Modifier) : IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.OUTERCLOTHING;
}

[ByRefEvent]
public record struct ShouldExtinguishInSpaceEvent(bool Cancelled = false) : IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.OUTERCLOTHING;
}

[ByRefEvent]
public record struct NoFirestacksUpdateEvent(EntityUid Uid, bool Handled = false) : IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.OUTERCLOTHING;
}

[ByRefEvent]
public record struct CanSeeOnCameraEvent(EntityUid Uid, bool Cancelled = false) : IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.OUTERCLOTHING;
}
