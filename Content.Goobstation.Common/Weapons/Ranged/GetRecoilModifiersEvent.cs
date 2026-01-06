namespace Content.Goobstation.Common.Weapons.Ranged;

[ByRefEvent]
public record struct GetRecoilModifiersEvent(EntityUid Gun, EntityUid User, float Modifier = 1f);
