using System.Linq;
using Content.Server.Actions;
using Content.Server.Administration.Logs;
using Content.Server.DoAfter;
using Content.Server.Jittering;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared._White.RadialSelector;
using Content.Shared._White.Xenomorphs;
using Content.Shared._White.Xenomorphs.Xenomorph;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

// Add this so the compiler knows about the Queen system
using Content.Server._White.Xenomorphs.Queen;

namespace Content.Server._White.Xenomorphs.Evolution;

/// <summary>
///     Handles the evolution of Xenomorphs from one caste to another.
///     This system manages UI, do-after timing, points accumulation, and evolution restrictions.
/// </summary>
public sealed class XenomorphEvolutionSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly JitteringSystem _jitter = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly XenomorphQueenSystem _queenSystem = default!; // <-- dependency for Queen checks

    public override void Initialize()
    {
        base.Initialize();

        // Subscribe to events for evolution actions and UI
        SubscribeLocalEvent<XenomorphEvolutionComponent, MapInitEvent>(OnXenomorphEvolutionMapInit);
        SubscribeLocalEvent<XenomorphEvolutionComponent, ComponentShutdown>(OnXenomorphEvolutionShutdown);
        SubscribeLocalEvent<XenomorphEvolutionComponent, EvolutionsActionEvent>(OnEvolutionsAction);
        SubscribeLocalEvent<XenomorphEvolutionComponent, RadialSelectorSelectedMessage>(OnEvolutionRecieved);
        SubscribeLocalEvent<XenomorphEvolutionComponent, XenomorphEvolutionDoAfterEvent>(OnXenomorphEvolutionDoAfter);
    }

    // Add the evolution action to the Xenomorph when it spawns
    private void OnXenomorphEvolutionMapInit(EntityUid uid, XenomorphEvolutionComponent component, MapInitEvent args) =>
        _actions.AddAction(uid, ref component.EvolutionAction, component.EvolutionActionId);

    // Remove the evolution action when the component is removed
    private void OnXenomorphEvolutionShutdown(EntityUid uid, XenomorphEvolutionComponent component, ComponentShutdown args) =>
        _actions.RemoveAction(uid, component.EvolutionAction);

    // Handle the radial selector or automatic evolution when the action is triggered
    private void OnEvolutionsAction(EntityUid uid, XenomorphEvolutionComponent component, ref EvolutionsActionEvent args)
    {
        if (args.Handled)
            return;

        if (component.EvolvesTo.Count == 1)
        {
            if (component.Points < component.Max)
            {
                _popup.PopupEntity(
                    Loc.GetString("xenomorphs-evolution-not-enough-points",
                    ("seconds", (component.Max - component.Points) / component.PointsPerSecond)), uid, uid);
                return;
            }

            args.Handled = Evolve(uid, component.EvolvesTo.First().Prototype, component.EvolutionDelay);
            return;
        }

        _ui.TryToggleUi(uid, RadialSelectorUiKey.Key, uid);
        _ui.SetUiState(uid, RadialSelectorUiKey.Key, new TrackedRadialSelectorState(component.EvolvesTo));

        args.Handled = true;
    }

    // Handle selection from the radial UI
    private void OnEvolutionRecieved(EntityUid uid, XenomorphEvolutionComponent component, RadialSelectorSelectedMessage args)
    {
        if (component.Points < component.Max)
        {
            _popup.PopupEntity(
                Loc.GetString("xenomorphs-evolution-not-enough-points",
                ("seconds", (component.Max - component.Points) / component.PointsPerSecond)), uid, uid);
            return;
        }

        if (Evolve(uid, args.SelectedItem, component.EvolutionDelay))
            return;

        var actor = args.Actor;
        _ui.CloseUi(uid, RadialSelectorUiKey.Key, actor);
    }

    // Handle the DoAfter for delayed evolution
    private void OnXenomorphEvolutionDoAfter(EntityUid uid, XenomorphEvolutionComponent component, ref XenomorphEvolutionDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || !_mind.TryGetMind(uid, out var mindUid, out var mind))
            return;

        var ev = new BeforeXenomorphEvolutionEvent(args.Caste);
        RaiseLocalEvent(uid, ev);

        if (ev.Cancelled)
            return;

        args.Handled = true;

        var coordinates = _transform.GetMoverCoordinates(uid);
        var newXeno = Spawn(args.Choice, coordinates);

        // Transfer mind from old Xenomorph to the new one
        _mind.TransferTo(mindUid, newXeno, mind: mind);
        _mind.UnVisit(mindUid, mind);

        // Drop hand items from old entity
        var dropHandItemsEvent = new DropHandItemsEvent();
        RaiseLocalEvent(uid, ref dropHandItemsEvent);
        RaiseLocalEvent(uid, new AfterXenomorphEvolutionEvent(newXeno, mindUid, args.Caste));

        _adminLog.Add(LogType.Mind, $"{ToPrettyString(uid)} evolved into {ToPrettyString(newXeno)}");

        Del(uid);

        _popup.PopupEntity(Loc.GetString("xenomorphs-evolution-end"), newXeno, newXeno);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;

        // Increment evolution points every second
        var query = EntityQueryEnumerator<XenomorphEvolutionComponent>();
        while (query.MoveNext(out var uid, out var alienEvolution))
        {
            if (alienEvolution.Points == alienEvolution.Max || time < alienEvolution.NextPointsAt || _container.IsEntityInContainer(uid))
                continue;

            alienEvolution.NextPointsAt = time + TimeSpan.FromSeconds(1);
            alienEvolution.Points += alienEvolution.PointsPerSecond;

            if (alienEvolution.Points != alienEvolution.Max)
                continue;

            // Notify player that evolution is ready
            _popup.PopupEntity(Loc.GetString("xenomorphs-evolution-ready"), uid, uid, PopupType.Large);
        }
    }

    /// <summary>
    ///     Attempt to evolve the Xenomorph to a new caste
    /// </summary>
    public bool Evolve(EntityUid uid, string? evolveTo, TimeSpan evolutionDelay, bool checkNeedCasteDeath = true)
    {
        if (evolveTo == null
            || !_protoManager.TryIndex(evolveTo, out var xenomorphPrototype)
            || !xenomorphPrototype.TryGetComponent<XenomorphComponent>(out var xenomorph, _componentFactory))
            return false;

        // Prevent evolving into Queen if a living Queen already exists
        if (xenomorph.Caste == "Queen" && _queenSystem.IsQueenAlive())
        {
            _popup.PopupEntity(
                Loc.GetString("xenomorphs-evolution-no-cast-slot", ("caste", "Queen")), uid);
            return false;
        }

        var ev = new BeforeXenomorphEvolutionEvent(xenomorph.Caste, checkNeedCasteDeath);
        RaiseLocalEvent(uid, ev);

        if (ev.Cancelled)
            return false;

        var doAfterEvent = new XenomorphEvolutionDoAfterEvent(evolveTo, xenomorph.Caste, checkNeedCasteDeath);
        var doAfter = new DoAfterArgs(EntityManager, uid, evolutionDelay, doAfterEvent, uid);

        if (!_doAfter.TryStartDoAfter(doAfter))
            return false;

        _jitter.DoJitter(uid, evolutionDelay, true, 80, 8, true);

        // Popups for nearby players
        var popupOthers = Loc.GetString("xenomorphs-evolution-start-others", ("uid", uid));
        _popup.PopupEntity(popupOthers, uid, Filter.PvsExcept(uid), true, PopupType.Medium);

        // Popup for self
        var popupSelf = Loc.GetString("xenomorphs-evolution-start-self");
        _popup.PopupEntity(popupSelf, uid, uid, PopupType.Medium);

        return true;
    }
}
