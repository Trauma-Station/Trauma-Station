using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Body.Components;

namespace Content.Shared.Body.Systems;

/// <summary>
/// Trauma - Provides missing API methods for bloodstream.
/// </summary>
public abstract partial class SharedBloodstreamSystem
{
    public void SetRefreshAmount(Entity<BloodstreamComponent> ent, FixedPoint2 amount)
    {
        ent.Comp.BloodRefreshAmount = amount;
        DirtyField(ent, nameof(BloodstreamComponent.BloodRefreshAmount));
    }
}
