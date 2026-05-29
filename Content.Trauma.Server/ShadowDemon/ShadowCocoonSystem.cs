// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Light.Components;
using Content.Server.Light.EntitySystems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Light.Components;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Content.Trauma.Shared.ShadowDemon.ShadowCocoon;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Trauma.Server.ShadowDemon;

public sealed partial class ShadowCocoonSystem : SharedShadowCocoonSystem
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedToolSystem _tool = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private PoweredLightSystem _poweredLight = default!;
    [Dependency] private EntityQuery<ExpendableLightComponent> _expendableLightQuery = default!;
    [Dependency] private EntityQuery<PoweredLightComponent> _poweredLightQuery = default!;
    [Dependency] private EntityQuery<WelderComponent> _welderQuery = default!;

    private readonly HashSet<Entity<PointLightComponent>> _lights = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        var eqe = EntityQueryEnumerator<ShadowCocoonComponent>();
        while (eqe.MoveNext(out var uid, out var cocoon))
        {
            if (now < cocoon.NextUpdate)
                return;

            cocoon.NextUpdate = now + cocoon.UpdateDelay;

            var coords = Transform(uid).Coordinates;
            _lights.Clear();
            _lookup.GetEntitiesInRange(coords, cocoon.Radius, _lights);
            foreach (var light in _lights)
            {
                if (!light.Comp.Enabled)
                    continue;

                var owner = light.Owner;
                if (_poweredLightQuery.TryComp(light, out var powered) && powered.On)
                {
                    // Destroy nearby light bulbs
                    _poweredLight.TryDestroyBulb(light, powered);
                    continue;
                }

                if (_welderQuery.TryComp(light, out var welder) && welder.Enabled)
                {
                    // Remove all fuel from the welder
                    if (!_solutionContainer.TryGetSolution(owner, welder.FuelSolutionName, out var solution))
                        continue;

                    var fuel = _tool.GetWelderFuelAndCapacity(light, welder);
                    _solutionContainer.RemoveReagent(solution.Value, welder.FuelReagent, fuel.fuel);
                    _tool.TurnOff((light, welder), uid);
                    continue;
                }

                if (_expendableLightQuery.TryComp(light, out var expandable))
                {
                    // Kill flare stuff
                    expandable.CurrentState = ExpendableLightState.Fading;
                    expandable.StateExpiryTime = 0;
                    Dirty(light, expandable);
                }
            }
        }
    }
}
