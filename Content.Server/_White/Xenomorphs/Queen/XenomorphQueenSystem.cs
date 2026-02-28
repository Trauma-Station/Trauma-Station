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

    // Goobstation start
    private void OnPromotionAction(EntityUid uid, XenomorphQueenComponent component, PromotionActionEvent args)
    {
        if (args.Target == EntityUid.Invalid || args.Target == args.Performer)
            return;
        // Additional validation in case the target is no longer valid
        if (!HasComp<XenomorphComponent>(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("xenomorphs-queen-promotion-invalid-target"), args.Performer, args.Performer);
            return;
        }

        if (!TryComp<XenomorphComponent>(args.Target, out var xenomorph))
            return;

        // Check if target is already a Praetorian or not in the whitelist
        if (xenomorph.Caste == PraetorianCaste || !component.CasteWhitelist.Contains(xenomorph.Caste))
        {
            if (xenomorph.Caste == PraetorianCaste)
                _popup.PopupEntity(Loc.GetString("xenomorphs-queen-already-praetorian"), args.Performer, args.Performer);
            else
                _popup.PopupEntity(Loc.GetString("xenomorphs-queen-promotion-didnt-pass-whitelist"), args.Performer, args.Performer);
            return;
        }

        if (xenomorph.Caste == QueenCaste && IsQueenAlive(args.Target))
        {
            _popup.PopupEntity(
                Loc.GetString("xenomorphs-evolution-no-cast-slot", ("caste", QueenCaste)), args.Performer, args.Performer);
            return;
        }

        // Try direct evolution with optional mind transfer
        var target = args.Target;
        var coordinates = Transform(target).Coordinates;
        var newXeno = Spawn(component.PromoteTo, coordinates);
        // Transfer mind if it exists
        if (_mind.TryGetMind(target, out var mindId, out var mind))
            _mind.TransferTo(mindId, newXeno, mind: mind);
        // Copy over any important components
        if (TryComp<XenomorphComponent>(newXeno, out var newXenoComp) &&
            TryComp<XenomorphComponent>(target, out var oldXenoComp))
        {
            newXenoComp.Caste = oldXenoComp.Caste;
        }

        // Update the caste to Praetorian for the new entity
        if (TryComp<XenomorphComponent>(newXeno, out var xenomorphComp))
        {
            xenomorphComp.Caste = PraetorianCaste;
            Dirty(newXeno, xenomorphComp);
        }

        // Get the target's name before deleting the entity
        var targetName = Name(target);

        // Clean up the old entity
        Del(target);

        // Deduct plasma cost if applicable
        _plasma.ChangePlasmaAmount(uid, -500f);
        _popup.PopupEntity(
            Loc.GetString("xenomorphs-queen-promotion-success", ("target", targetName)), uid, uid);

        args.Handled = true;
    }

    public bool IsQueenAlive(EntityUid caller)
    {
        var callerMap = Transform(caller).MapID;
        var query = EntityQueryEnumerator<XenomorphQueenComponent, MindContainerComponent>();
        while (query.MoveNext(out var uid, out _, out var mindContainer))
        {
            if (Exists(uid) && mindContainer.HasMind && Transform(uid).MapID == callerMap)
                return true;
        }
        return false;
        // Goobstation end
    }
}
