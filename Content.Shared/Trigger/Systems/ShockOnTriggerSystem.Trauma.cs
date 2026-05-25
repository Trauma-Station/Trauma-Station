using Content.Shared.Power.EntitySystems;

namespace Content.Shared.Trigger.Systems;

public sealed partial class ShockOnTriggerSystem
{
    [Dependency] private SharedBatterySystem _battery = default!;
}
