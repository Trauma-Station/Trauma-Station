// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.Systems;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Forging;

public sealed partial class WorkableSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedMetalSystem _metal = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTemperatureSystem _temp = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private EntityQuery<WorkableComponent> _query = default!;

    // TODO: quality integration

    [SubscribeLocalEvent]
    private void OnDamageDealt(Entity<WorkableComponent> ent, ref DamageDealtEvent args)
    {
        if (TerminatingOrDeleted(ent) ||
            !_timing.IsFirstTimePredicted ||
            args.Origin is not {} user || // random explosion can't forge something, youd need a really really specific shaped charge
            !args.Damage.DamageDict.TryGetValue(ent.Comp.DamageType, out var dealt) ||
            dealt < FixedPoint2.Zero)
            return;

        if (!_metal.IsWorkable(ent.Owner))
        {
            _popup.PopupEntity(Loc.GetString("workable-metal-popup-too-cold"), user, user);
            return;
        }

        // TODO: require being on an anvil for plasteel or forging items from ingots.

        ent.Comp.Remaining -= dealt;
        if (ent.Comp.Remaining <= FixedPoint2.Zero)
            CreateResult(ent, user);
        else
            DirtyField(ent, ent.Comp, nameof(WorkableComponent.Remaining));
    }

    [SubscribeLocalEvent]
    private void OnExamined(Entity<WorkableComponent> ent, ref ExaminedEvent args)
    {
        // TODO: add a skill check for knowing if its workable by eye
        if (!args.IsInDetailsRange)
            return;

        var workable = _metal.IsWorkable(ent.Owner);
        args.PushMarkup(Loc.GetString("workable-metal-examine", ("workable", workable)));
    }

    [SubscribeLocalEvent]
    private void OnTemperatureWrought(Entity<TemperatureComponent> ent, ref MetalWroughtEvent args)
    {
        _temp.SetTemperature(args.Result, ent.Comp.Temperature);
    }

    private void CreateResult(Entity<WorkableComponent> ent, EntityUid? user)
    {
        var xform = Transform(ent);
        for (var i = 0; i < ent.Comp.Amount; i++)
        {
            var result = PredictedSpawnAtPosition(ent.Comp.Result, xform.Coordinates);
            _transform.SetLocalRotation(result, xform.LocalRotation);
            var ev = new MetalWroughtEvent(result, user);
            RaiseLocalEvent(ent, ref ev);
        }
        PredictedQueueDel(ent);

        ent.Comp.Amount = 0; // incase damage is changed multiple times in the same tick
    }

    public void SetRemaining(Entity<WorkableComponent?> ent, FixedPoint2 value)
    {
        if (!_query.Resolve(ent, ref ent.Comp) || ent.Comp.Remaining == value)
            return;

        ent.Comp.Remaining = value;
        DirtyField(ent, ent.Comp, nameof(WorkableComponent.Remaining));
    }

    public void SetResult(Entity<WorkableComponent?> ent, [ForbidLiteral] EntProtoId id)
    {
        if (!_query.Resolve(ent, ref ent.Comp) || ent.Comp.Result == id)
            return;

        ent.Comp.Result = id;
        DirtyField(ent, ent.Comp, nameof(WorkableComponent.Result));
    }

    public void SetAmount(Entity<WorkableComponent?> ent, int amount)
    {
        if (!_query.Resolve(ent, ref ent.Comp) || ent.Comp.Amount == amount)
            return;

        ent.Comp.Amount = amount;
        DirtyField(ent, ent.Comp, nameof(WorkableComponent.Amount));
    }
}
