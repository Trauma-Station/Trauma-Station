// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Factory.Common.Construction;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Examine;
using Robust.Shared.Map;

namespace Content.Factory.Shared.Machines;

public abstract partial class ConstructorSystem : EntitySystem
{
    [Dependency] protected ISharedAdminLogManager _adminLogger = default!;
    [Dependency] protected SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<ConstructorComponent>(ConstructorUiKey.Key, subs =>
        {
            subs.Event<ConstructorSetProtoMessage>(OnSetProto);
        });
    }

    [SubscribeLocalEvent]
    private void OnExamined(Entity<ConstructorComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var msg = ent.Comp.Construction is {} id
            ? Loc.GetString("constructor-examine", ("name", ProtoMan.Index(id).Name ?? id))
            : Loc.GetString("constructor-examine-unset");
        args.PushMarkup(msg);
    }

    [SubscribeLocalEvent]
    private void OnConstructed(Entity<ConstructorComponent> ent, ref ConstructedEvent args)
    {
        _transform.SetCoordinates(args.Entity, OutputPosition(ent));
    }

    private void OnSetProto(Entity<ConstructorComponent> ent, ref ConstructorSetProtoMessage args)
    {
        if (ent.Comp.Construction == args.Id
            || !ProtoMan.HasIndex(args.Id))
            return;

        ent.Comp.Construction = args.Id;
        Dirty(ent);
        _adminLogger.Add(LogType.Construction, LogImpact.Low, $"{args.Actor:user} set {ent.Owner:target} construction to {args.Id}");
    }

    public EntityCoordinates OutputPosition(EntityUid uid)
    {
        var xform = Transform(uid);
        var offset = xform.LocalRotation.ToVec();
        return xform.Coordinates.Offset(offset);
    }
}
