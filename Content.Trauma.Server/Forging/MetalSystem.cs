// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Temperature.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Temperature;
using Content.Trauma.Shared.Forging;

namespace Content.Trauma.Server.Forging;

public sealed class MetalSystem : SharedMetalSystem
{
    [Dependency] private readonly DamageOnHoldingSystem _damageOnHolding = default!;
    [Dependency] private readonly EntityQuery<InternalTemperatureComponent> _internalQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MetallicComponent, OnTemperatureChangeEvent>(OnTemperatureChange);

        SubscribeLocalEvent<InternalTemperatureComponent, MetalWroughtEvent>(OnInternalWrought);
    }

    private void OnTemperatureChange(Entity<MetallicComponent> ent, ref OnTemperatureChangeEvent args)
    {
        // skin temperature for damage because thats what you would touch
        _damageOnHolding.SetEnabled(ent.Owner, args.CurrentTemperature > ent.Comp.DamageHoldingTemp);

        // only using internal temperature for workability
        if (!_internalQuery.TryComp(ent, out var comp))
            return;

        var t = comp.Temperature;
        if (ent.Comp.Workable)
            TryCool(ent, t);
        else
            TryHeat(ent, t);
    }

    private void OnInternalWrought(Entity<InternalTemperatureComponent> ent, ref MetalWroughtEvent args)
    {
        if (!_internalQuery.TryComp(args.Result, out var dest))
            return;

        dest.Temperature = ent.Comp.Temperature;
    }

    private void TryCool(Entity<MetallicComponent> ent, float t)
    {
        if (t < ent.Comp.MinTemp)
            SetWorkable(ent, false);
    }

    private void TryHeat(Entity<MetallicComponent> ent, float t)
    {
        if (t >= ent.Comp.IdealTemp)
            SetWorkable(ent, true);
    }
}
