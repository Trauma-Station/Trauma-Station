using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Destructible;
using Content.Shared.Destructible.Thresholds.Behaviors;
using JetBrains.Annotations;

namespace Content.Server.Destructible.Thresholds.Behaviors;

[UsedImplicitly]
[DataDefinition]
public sealed partial class SpawnGasBehavior : IThresholdBehavior
{
    [DataField("gasMixture", required: true)]
    public GasMixture Gas = new();

    public void Execute(EntityUid owner, SharedDestructibleSystem system, EntityUid? cause = null)
    {
        var atmos = system.EntityManager.System<AtmosphereSystem>();
        var air = atmos.GetContainingMixture(owner, false, true);

        if (air != null)
            atmos.Merge(air, Gas);
    }
}
