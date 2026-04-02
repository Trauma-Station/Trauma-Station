// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Construction.Components;
using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.Temperature;
using Content.Shared.Temperature.Components;
using Content.Trauma.Shared.Forging;

namespace Content.Trauma.Server.Forging;

/// <summary>
/// All the serverside metal, working and forging logic.
/// Only exists because temperature and construction shitcode is serverside.
/// </summary>
public sealed class MetalSystem : SharedMetalSystem
{
    [Dependency] private readonly DamageOnHoldingSystem _damageOnHolding = default!;
    [Dependency] private readonly TemperatureSystem _temperature = default!;
    [Dependency] private readonly EntityQuery<InternalTemperatureComponent> _internalQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MetallicComponent, OnTemperatureChangeEvent>(OnTemperatureChange);

        SubscribeLocalEvent<InternalTemperatureComponent, MetalWroughtEvent>(OnInternalWrought);

        SubscribeLocalEvent<TemperatureComponent, ItemForgedEvent>(OnTemperatureForged);

        SubscribeLocalEvent<ForgedItemComponent, ForgingCompletedEvent>(OnForgingCompleted);
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

    private void OnTemperatureForged(Entity<TemperatureComponent> ent, ref ItemForgedEvent args)
    {
        // TODO: base it off your input metals if that matters in the future
        var temp = Proto.Index(GetMetalOrThrow(ent)).WorkingTemp;
        _temperature.ForceChangeTemperature(ent, temp, ent.Comp);
        if (_internalQuery.TryComp(ent, out var comp))
            comp.Temperature = temp;
    }

    private void OnForgingCompleted(Entity<ForgedItemComponent> ent, ref ForgingCompletedEvent args)
    {
        if (args.Item.Construction is not {} graph)
            return;

        var comp = EnsureComp<ConstructionComponent>(ent);
        comp.Graph = graph;
        comp.Node = "start";
        comp.TargetNode = "finished"; // have to set this as the end node in every procgen's graph
        comp.EdgeIndex = 0; // say to quench it
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
