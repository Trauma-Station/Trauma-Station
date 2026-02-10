// <Trauma>
using Content.Shared.Damage;
// </Trauma>
using System.Numerics;
using Content.Shared.Inventory;
using Content.Shared.Weapons.Reflect;

namespace Content.Shared.Weapons.Ranged.Events;

/// <summary>
/// Shot may be reflected by setting <see cref="Reflected"/> to true
/// and changing <see cref="Direction"/> where shot will go next
/// </summary>
[ByRefEvent]
// Trauma - add Damage
public record struct HitScanReflectAttemptEvent(EntityUid? Shooter, EntityUid SourceItem, ReflectType Reflective, Vector2 Direction, bool Reflected, DamageSpecifier? Damage) : IInventoryRelayEvent
{
    SlotFlags IInventoryRelayEvent.TargetSlots => SlotFlags.WITHOUT_POCKET;
}
