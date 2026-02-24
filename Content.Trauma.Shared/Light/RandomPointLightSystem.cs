// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Random.Helpers;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Light;

public sealed class RandomPointLightSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomPointLightComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<RandomPointLightComponent> ent, ref MapInitEvent args)
    {
        if (!_light.TryGetLight(ent, out var light))
            return;

        var seed = SharedRandomExtensions.HashCodeCombine((int) _timing.CurTick.Value, GetNetEntity(ent).Id);
        var rand = new Random(seed);
        var color = rand.Pick(ent.Comp.Colors);
        var energy = rand.NextFloat(ent.Comp.Energy.X, ent.Comp.Energy.Y);
        _light.SetColor(ent.Owner, color, light);
        _light.SetEnergy(ent.Owner, energy, light);
    }
}
