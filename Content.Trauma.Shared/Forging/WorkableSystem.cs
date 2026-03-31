// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Content.Shared.Temperature;
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
        SubscribeLocalEvent<WorkableComponent, ExaminedEvent>(OnExamined);

        SubscribeLocalEvent<TemperatureComponent, MetalWroughtEvent>(OnTemperatureWrought);
        // TODO: quality integration
    }

    private void OnDamageChanged(Entity<WorkableComponent> ent, ref DamageChangedEvent args)
    {
        if (TerminatingOrDeleted(ent) ||
            args.DamageDelta is not {} delta ||
            args.Origin is not {} user || // random explosion can't forge something, youd need a really really specific shaped charge
            !delta.DamageDict.TryGetValue(ent.Comp.DamageType, out var dealt))
            return;

        if (!_metal.IsWorkable(ent.Owner))
        {
            _popup.PopupClient(Loc.GetString("workable-metal-popup-too-cold"), user, user);
            return;
        }

        ent.Comp.Remaining -= dealt;
        if (ent.Comp.Remaining <= FixedPoint2.Zero)
            CreateResult(ent);
        else
            Dirty(ent);
    }

    private void OnExamined(Entity<WorkableComponent> ent, ref ExaminedEvent args)
    {
        // TODO: add a skill check for knowing if its workable by eye
        if (!args.IsInDetailsRange)
            return;

        var workable = _metal.IsWorkable(ent.Owner);
        args.PushMarkup(Loc.GetString("workable-metal-examine", ("workable", workable)));
    }

    private void OnTemperatureWrought(Entity<TemperatureComponent> ent, ref MetalWroughtEvent args)
    {
        if (!TryComp<TemperatureComponent>(args.Result, out var dest))
            return;

        dest.CurrentTemperature = ent.Comp.CurrentTemperature;
        var ev = new OnTemperatureChangeEvent(dest.CurrentTemperature, dest.CurrentTemperature, 0);
        RaiseLocalEvent(args.Result, ev);
    }

    private void CreateResult(Entity<WorkableComponent> ent)
    {
        var xform = Transform(ent);
        for (var i = 0; i < ent.Comp.Amount; i++)
        {
            var result = PredictedSpawnAtPosition(ent.Comp.Result, xform.Coordinates);
            _transform.SetLocalRotation(result, xform.LocalRotation);
            var ev = new MetalWroughtEvent(result);
            RaiseLocalEvent(ent, ref ev);
        }
        PredictedQueueDel(ent);

        ent.Comp.Amount = 0; // incase damage is changed multiple times in the same tick
    }
}
