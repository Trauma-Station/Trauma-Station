using Content.Shared.Silicons.Laws.Components;

namespace Content.Shared.Silicons.Laws;

public abstract partial class SharedSiliconLawSystem
{
    public virtual void TriggerIonStorm(Entity<SiliconLawBoundComponent?, IonStormTargetComponent?> target) { }
}
