using Content.Goobstation.Common.CCVar;
using Content.Shared.FixedPoint;
using Content.Shared.Body.Components;
using Robust.Shared.Configuration;

namespace Content.Shared.Body.Systems;

/// <summary>
/// Trauma - Provides missing API methods for bloodstream.
/// </summary>
public abstract partial class SharedBloodstreamSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private float _bloodlossMultiplier = 4f;

    private void InitializeTrauma()
    {
        Subs.CVar(_cfg, GoobCVars.BleedMultiplier, value => _bloodlossMultiplier = value, true);
    }

    public void SetRefreshAmount(Entity<BloodstreamComponent> ent, FixedPoint2 amount)
    {
        ent.Comp.BloodRefreshAmount = amount;
        DirtyField(ent.AsNullable(), nameof(BloodstreamComponent.BloodRefreshAmount));
    }
}
