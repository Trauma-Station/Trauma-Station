// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Content.Shared.Temperature.Components;

namespace Content.Trauma.Shared.Forging;

public sealed class WorkableSystem : EntitySystem
{
    [Dependency] private readonly SharedMetalSystem _metal = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WorkableComponent, DamageChangedEvent>(OnDamageChanged);

        SubscribeLocalEvent<TemperatureComponent, MetalWroughtEvent>(OnTemperatureWrought);
    }

    private void OnDamageChanged(Entity<WorkableComponent> ent, ref DamageChangedEvent args)
    {
        if (TerminatingOrDeleted(ent) ||
            args.DamageDelta is not {} delta ||
            !delta.DamageDict.TryGetValue(ent.Comp.DamageType, out var dealt))
            return;

        if (!_metal.IsWorkable(ent.Owner))
        {
            if (args.Origin is {} user) // hopefully this is a player :)
                _popup.PopupClient(Loc.GetString("workable-metal-too-cold"), user, user);
            return;
        }

        ent.Comp.Remaining -= dealt;
        if (ent.Comp.Remaining <= FixedPoint2.Zero)
            CreateResult(ent);
        else
            Dirty(ent);
    }

    private void OnTemperatureWrought(Entity<TemperatureComponent> ent, ref MetalWroughtEvent args)
    {
        if (!TryComp<TemperatureComponent>(args.Result, out var dest))
            return;

        dest.CurrentTemperature = ent.Comp.CurrentTemperature;
    }

    private void CreateResult(Entity<WorkableComponent> ent)
    {
        ent.Comp.Remaining = FixedPoint2.MaxValue; // incase damage is changed multiple times in the same tick

        var xform = Transform(ent);
        var result = PredictedSpawnAtPosition(ent.Comp.Result, xform.Coordinates);
        _transform.SetLocalRotation(result, xform.LocalRotation);
        var ev = new MetalWroughtEvent(result);
        RaiseLocalEvent(ent, ref ev);
        PredictedQueueDel(ent);
    }
}
