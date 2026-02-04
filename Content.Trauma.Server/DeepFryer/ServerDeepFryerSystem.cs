using Content.Shared.Chemistry.EntitySystems;
using Content.Trauma.Shared.DeepFryer.Components;
using Content.Trauma.Shared.DeepFryer.Systems;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.DeepFryer;

public sealed class ServerDeepFryerSystem : DeepFryerSystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query1 = EntityQueryEnumerator<DeepFryerComponent>();
        while (query1.MoveNext(out var fryerUid, out var fryer))
        {
            if (!fryer.Closed)
                continue;

            AddHeatToSolution((fryerUid, fryer), frameTime);

            if (fryer.StoredObjects.Count == 0)
                continue;

            AddHeatDamage((fryerUid, fryer), frameTime);

            if (fryer.FryFinishTime < _gameTiming.CurTime && fryer.FryFinishTime != TimeSpan.Zero)
            {
                DeepFryItems((fryerUid,fryer));
            }
        }
    }

    private void AddHeatToSolution(Entity<DeepFryerComponent> ent, float frameTime)
    {
        if (_solutionContainer.TryGetSolution(ent.Owner,
                ent.Comp.FryerSolutionContainer,
                out var solutionRef,
                out _))
        {
            _solutionContainer.AddThermalEnergyClamped(solutionRef.Value, ent.Comp.HeatToAddToSolution * frameTime, 0f, ent.Comp.MaxHeat);
        }
    }
}
