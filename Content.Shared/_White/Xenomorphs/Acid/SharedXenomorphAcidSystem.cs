using Content.Shared._White.Actions;
using Content.Shared._White.Other;
using Content.Shared._White.Xenomorphs.Acid.Components;
using Content.Shared.Coordinates;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Shared._White.Xenomorphs.Acid;

public abstract class SharedXenomorphAcidSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenomorphAcidComponent, AcidActionEvent>(OnAcidAction);
    }

    private void OnAcidAction(Entity<XenomorphAcidComponent> ent, ref AcidActionEvent args)
    {
        if (args.Handled)
            return;

        var comp = ent.Comp;
        var user = args.Performer;
        var target = Identity.Entity(args.Target, EntityManager);

        // Check if this is a plasma-cost action and get the cost
        if (!HasComp<StructureComponent>(args.Target)) // TODO: This should check whether the target is a structure.
        {
            _popup.PopupClient(Loc.GetString("xenomorphs-acid-not-corrodible", ("target", target)), user, user, PopupType.SmallCaution);
            return;
        }

        if (HasComp<AcidCorrodingComponent>(args.Target))
        {
            _popup.PopupClient(Loc.GetString("xenomorphs-acid-already-corroding", ("target", target)), user, user, PopupType.SmallCaution);
            return;
        }

        args.Handled = true;
        _popup.PopupClient(Loc.GetString("xenomorphs-acid-apply", ("target", target)), user, user);

        var acid = PredictedSpawnAttachedTo(comp.AcidId, args.Target.ToCoordinates());
        var acidCorroding = new AcidCorrodingComponent
        {
            Acid = acid,
            AcidExpiresAt = Timing.CurTime + comp.AcidLifeTime,
            DamagePerSecond = comp.DamagePerSecond
        };
        AddComp(args.Target, acidCorroding);
    }
}
