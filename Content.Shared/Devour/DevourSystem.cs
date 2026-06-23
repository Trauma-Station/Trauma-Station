using Content.Goobstation.Common.Devour;
using Content.Shared.Actions;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Destructible;
using Content.Shared.Devour.Components;
using Content.Shared.DoAfter;
using Content.Shared.Gibbing;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;

namespace Content.Shared.Devour;

public sealed partial class DevourSystem : EntitySystem
{
    [Dependency] private EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private SharedActionsSystem _actionsSystem = default!;
    [Dependency] private SharedAudioSystem _audioSystem = default!;
    [Dependency] private SharedBloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private SharedContainerSystem _containerSystem = default!;
    [Dependency] private SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private SharedPopupSystem _popupSystem = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!; // Goobstation
    [Dependency] private SharedDestructibleSystem _destructible = default!; // Orion

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DevourerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<DevourerComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<DevourerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<DevourerComponent, DevourActionEvent>(OnDevourAction);
        SubscribeLocalEvent<DevourerComponent, DevourDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<DevourerComponent, GibbedBeforeDeletionEvent>(OnGibContents);
    }

    private void OnStartup(Entity<DevourerComponent> ent, ref ComponentStartup args)
    {
        //Devourer doesn't actually chew, since he sends targets right into his stomach.
        //I did it mom, I added ERP content into upstream. Legally!
        ent.Comp.Stomach = _containerSystem.EnsureContainer<Container>(ent.Owner, DevourerComponent.StomachContainerId);
    }

    private void OnInit(Entity<DevourerComponent> ent, ref MapInitEvent args)
    {
        _actionsSystem.AddAction(ent.Owner, ref ent.Comp.DevourActionEntity, ent.Comp.DevourAction);
    }

    private void OnShutdown(Entity<DevourerComponent> ent, ref ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(ent.Owner, ent.Comp.DevourActionEntity);
    }

    /// <summary>
    /// The devour action
    /// </summary>
    private void OnDevourAction(Entity<DevourerComponent> ent, ref DevourActionEvent args)
    {
        if (args.Handled || _whitelistSystem.IsWhitelistFailOrNull(ent.Comp.Whitelist, args.Target))
            return;

        args.Handled = true;
        var target = args.Target;

        // Structure and mob devours handled differently.
        if (TryComp(target, out MobStateComponent? targetState))
        {
            switch (targetState.CurrentState)
            {
                case MobState.Critical:
                case MobState.Dead:

                    _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, ent.Owner, ent.Comp.DevourTime, new DevourDoAfterEvent(), ent.Owner, target: target, used: ent.Owner)
                    {
                        BreakOnMove = true,
                    });
                    break;
                case MobState.Invalid:
                case MobState.Alive:
                default:
                    _popupSystem.PopupEntity(Loc.GetString("devour-action-popup-message-fail-target-alive"), ent.Owner, ent.Owner);
                    break;
            }

            return;
        }

        // Orion-Start
        if (IsIndestructibleStructure(target))
        {
            _popupSystem.PopupClient(Loc.GetString("devour-action-popup-message-fail-target-indestructible"), ent.Owner, ent.Owner);
            return;
        }
        // Orion-End

        _popupSystem.PopupEntity(Loc.GetString("devour-action-popup-message-structure"), ent.Owner, ent.Owner);

        // Orion-Start
        var canDevourStructure = new DestructionAttemptEvent();
        RaiseLocalEvent(target, canDevourStructure);
        if (canDevourStructure.Cancelled)
        {
            _popupSystem.PopupEntity(Loc.GetString("devour-action-popup-message-fail-target-indestructible"), ent.Owner, ent.Owner);
            return;
        }
        // Orion-End

        if (ent.Comp.SoundStructureDevour != null)
            _audioSystem.PlayPredicted(ent.Comp.SoundStructureDevour, ent.Owner, ent.Owner, ent.Comp.SoundStructureDevour.Params);

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, ent.Owner, ent.Comp.StructureDevourTime, new DevourDoAfterEvent(), ent.Owner, target: target, used: ent.Owner)
        {
            BreakOnMove = true,
        });
    }

    private void OnDoAfter(Entity<DevourerComponent> ent, ref DevourDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

/* // Orion-Edit: Moved down
        var ichorInjection = new Solution(ent.Comp.Chemical, ent.Comp.HealRate);

        // Grant ichor if the devoured thing meets the dragon's food preference
        if (args.Args.Target != null && _whitelistSystem.IsWhitelistPassOrNull(ent.Comp.FoodPreferenceWhitelist, (EntityUid)args.Args.Target))
        {
            _bloodstreamSystem.TryAddToBloodstream(ent.Owner, ichorInjection);
        }
        // <Goobstation> voring walls is good for iron intake
        if (args.Args.Target is {} target && _solution.TryGetSolution(target, "food", out _, out var food))
            _bloodstreamSystem.TryAddToBloodstream(ent.Owner, food);
        // </Goobstation>
*/

        // Orion-Start
        if (args.Args.Target == null)
            return;
        // Orion-End

        // If the devoured thing meets the stomach whitelist criteria, add it to the stomach
        if (_whitelistSystem.IsWhitelistPass(ent.Comp.StomachStorageWhitelist, args.Args.Target.Value)) // Orion-Edit
        {
            _containerSystem.Insert(args.Args.Target.Value, ent.Comp.Stomach);

            // Goobstation start

            if (HasComp<MobStateComponent>(args.Args.Target.Value)) // can be cases where objects are also whitelisted, which wont need this
                EnsureComp<PreventSelfRevivalComponent>(args.Args.Target.Value);

            // Goobstation end

        }
        //TODO: Figure out a better way of removing structures via devour that still entails standing still and waiting for a DoAfter. Somehow.
        //If it's not alive, it must be a structure.
        // Delete if the thing isn't in the stomach storage whitelist (or the stomach whitelist is null/empty)
        else // Orion-Edit
        {
//            PredictedQueueDel(args.Args.Target.Value); // Orion-Edit

            // Orion-Start: Protect indestructible from devour
            if (IsIndestructibleStructure(args.Args.Target.Value))
            {
                _popupSystem.PopupClient(Loc.GetString("devour-action-popup-message-fail-target-indestructible"), ent.Owner, ent.Owner);
                return;
            }

            if (!_destructible.DestroyEntity(args.Args.Target.Value))
            {
                _popupSystem.PopupClient(Loc.GetString("devour-action-popup-message-fail-target-indestructible"), ent.Owner, ent.Owner);
                return;
            }
            // Orion-End
        }

        // Orion-Start
        var ichorInjection = new Solution(ent.Comp.Chemical, ent.Comp.HealRate);

        // Grant ichor if the devoured thing meets the dragon's food preference
        if (_whitelistSystem.IsWhitelistPassOrNull(ent.Comp.FoodPreferenceWhitelist, args.Args.Target.Value))
            _bloodstreamSystem.TryAddToBloodstream(ent.Owner, ichorInjection);

        // <Goobstation> voring walls is good for iron intake
        if (_solution.TryGetSolution(args.Args.Target.Value, "food", out _, out var food))
            _bloodstreamSystem.TryAddToBloodstream(ent.Owner, food);
        // </Goobstation>
        // Orion-End

        _audioSystem.PlayPredicted(ent.Comp.SoundDevour, ent.Owner, ent.Owner);
    }

    // Orion-Start
    private bool IsIndestructibleStructure(EntityUid target)
    {
        var prototypeId = MetaData(target).EntityPrototype?.ID;
        return prototypeId != null && prototypeId.Contains("Indestructible", StringComparison.OrdinalIgnoreCase);
    }
    // Orion-End

    private void OnGibContents(Entity<DevourerComponent> ent, ref GibbedBeforeDeletionEvent args)
    {
        if (ent.Comp.StomachStorageWhitelist == null)
            return;

        // <Goob>
        foreach (var entity in ent.Comp.Stomach.ContainedEntities)
        {
            RemComp<PreventSelfRevivalComponent>(entity);
        }
        _containerSystem.EmptyContainer(ent.Comp.Stomach);
    }
}

public sealed partial class DevourActionEvent : EntityTargetActionEvent;

[Serializable, NetSerializable]
public sealed partial class DevourDoAfterEvent : SimpleDoAfterEvent;
