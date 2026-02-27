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
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Subscribe to promotion action events
        SubscribeLocalEvent<XenomorphQueenComponent, PromotionActionEvent>(OnPromotionAction);
        // Setup queen on map initialization
        SubscribeLocalEvent<XenomorphQueenComponent, MapInitEvent>(OnMapInit);
        // Cleanup actions when component is removed
        SubscribeLocalEvent<XenomorphQueenComponent, ComponentShutdown>(OnShutdown);
    }

    // Add the promotion action to the Queen when it spawns
    private void OnMapInit(EntityUid uid, XenomorphQueenComponent component, MapInitEvent args) =>
        _actions.AddAction(uid, ref component.PromotionAction, component.PromotionActionId);

    // Remove the promotion action when the component shuts down
    private void OnShutdown(EntityUid uid, XenomorphQueenComponent component, ComponentShutdown args) =>
        _actions.RemoveAction(uid, component.PromotionAction);

    // Handle the promotion of a Xenomorph into a Praetorian by the Queen
    private void OnPromotionAction(EntityUid uid, XenomorphQueenComponent component, PromotionActionEvent args)
    {
        // Ignore invalid targets or self-targets
        if (args.Target == EntityUid.Invalid || args.Target == args.Performer)
            return;

        // Validate that the target is a Xenomorph
        if (!HasComp<XenomorphComponent>(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("xenomorphs-queen-promotion-invalid-target"), args.Performer);
            return;
        }

        if (!TryComp<XenomorphComponent>(args.Target, out var xenomorph))
            return;

        // Check if target is already a Praetorian or not in the whitelist
        if (xenomorph.Caste == "Praetorian" || !component.CasteWhitelist.Contains(xenomorph.Caste))
        {
            if (xenomorph.Caste == "Praetorian")
                _popup.PopupEntity(Loc.GetString("xenomorphs-queen-already-praetorian"), args.Performer);
            else
                _popup.PopupEntity(Loc.GetString("xenomorphs-queen-promotion-didnt-pass-whitelist"), args.Performer);
            return;
        }

        // Prevent promoting to Queen if a living Queen exists
        if (xenomorph.Caste == "Queen" && _entityManager.System<XenomorphQueenSystem>().IsQueenAlive())
        {
            _popup.PopupEntity(
                Loc.GetString("xenomorphs-evolution-no-cast-slot", ("caste", "Queen")), args.Performer);
            return;
        }

        // Spawn the new Praetorian at the target's coordinates
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
            xenomorphComp.Caste = "Praetorian";
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

    /// <summary>
    ///     Returns true if there is at least one living Xenomorph Queen on the station
    /// </summary>
    public bool IsQueenAlive()
    {
        var query = EntityQueryEnumerator<XenomorphQueenComponent, DamageableComponent>();
        while (query.MoveNext(out var uid, out var queen, out var damageable))
        {
            // Check if the Queen has taken any damage that would indicate she's dead
            if (damageable.TotalDamage < 1) // Alive if TotalDamage is zero
                return true;
        }

        return false;
    }
}
