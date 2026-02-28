using Content.Server._White.Xenomorphs.Evolution;
using Content.Server._White.Xenomorphs.Plasma;
using Content.Server.Actions;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Shared._White.Actions;
using Content.Shared._White.Xenomorphs;
using Content.Shared._White.Xenomorphs.Queen;
using Content.Shared._White.Xenomorphs.Xenomorph;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;

namespace Content.Server._White.Xenomorphs.Queen;

/// <summary>
///     Handles the behavior of the Xenomorph Queen.
///     Responsible for promotion actions, spawning Praetorians, and plasma costs.
/// </summary>
public sealed class XenomorphQueenSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly PlasmaSystem _plasma = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly XenomorphEvolutionSystem _xenomorphEvolution = default!;

    private static readonly ProtoId<EntityPrototype> QueenCaste = "Queen";
    private static readonly ProtoId<EntityPrototype> PraetorianCaste = "Praetorian";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenomorphQueenComponent, PromotionActionEvent>(OnPromotionAction);
        SubscribeLocalEvent<XenomorphQueenComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<XenomorphQueenComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnMapInit(EntityUid uid, XenomorphQueenComponent component, MapInitEvent args) =>
        _actions.AddAction(uid, ref component.PromotionAction, component.PromotionActionId);

    private void OnShutdown(EntityUid uid, XenomorphQueenComponent component, ComponentShutdown args) =>
        _actions.RemoveAction(uid, component.PromotionAction);

    private void OnPromotionAction(EntityUid uid, XenomorphQueenComponent component, PromotionActionEvent args)
    {
        if (args.Target == EntityUid.Invalid || args.Target == args.Performer)
            return;

        if (!HasComp<XenomorphComponent>(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("xenomorphs-queen-promotion-invalid-target"), args.Performer, args.Performer);
            return;
        }

        if (!TryComp<XenomorphComponent>(args.Target, out var xenomorph))
            return;

        if (xenomorph.Caste == PraetorianCaste || !component.CasteWhitelist.Contains(xenomorph.Caste))
        {
            if (xenomorph.Caste == PraetorianCaste)
                _popup.PopupEntity(Loc.GetString("xenomorphs-queen-already-praetorian"), args.Performer, args.Performer);
            else
                _popup.PopupEntity(Loc.GetString("xenomorphs-queen-promotion-didnt-pass-whitelist"), args.Performer, args.Performer);
            return;
        }

        if (xenomorph.Caste == QueenCaste && IsQueenAlive())
        {
            _popup.PopupEntity(
                Loc.GetString("xenomorphs-evolution-no-cast-slot", ("caste", QueenCaste)), args.Performer, args.Performer);
            return;
        }

        var target = args.Target;
        var coordinates = Transform(target).Coordinates;
        var newXeno = Spawn(component.PromoteTo, coordinates);

        if (_mind.TryGetMind(target, out var mindId, out var mind))
            _mind.TransferTo(mindId, newXeno, mind: mind);

        if (TryComp<XenomorphComponent>(newXeno, out var newXenoComp) &&
            TryComp<XenomorphComponent>(target, out var oldXenoComp))
        {
            newXenoComp.Caste = oldXenoComp.Caste;
        }

        if (TryComp<XenomorphComponent>(newXeno, out var xenomorphComp))
        {
            xenomorphComp.Caste = PraetorianCaste;
            Dirty(newXeno, xenomorphComp);
        }

        var targetName = Name(target);

        Del(target);

        _plasma.ChangePlasmaAmount(uid, -500f);
        _popup.PopupEntity(
            Loc.GetString("xenomorphs-queen-promotion-success", ("target", targetName)), uid, uid);

        args.Handled = true;
    }

    /// <summary>
    ///     Returns true if there is at least one living Xenomorph Queen on the station
    /// </summary>
    public bool IsQueenAlive()
    {
        var query = EntityQueryEnumerator<XenomorphQueenComponent, MindContainerComponent>();
        while (query.MoveNext(out var uid, out _, out var mindContainer))
        {
            if (Exists(uid) && mindContainer.HasMind)
                return true;
        }
        return false;
    }
}
