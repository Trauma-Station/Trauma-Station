// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.DoAfter;
using Content.Goobstation.Shared.Factory.Filters;
using Content.Shared.CombatMode;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Content.Shared.Tools;
using Content.Shared.Tools.Systems;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;

namespace Content.Goobstation.Shared.Factory;

internal delegate bool Toggle();

public abstract partial class SharedInteractorSystem : EntitySystem
{
    [Dependency] private AutomationSystem _automation = default!;
    [Dependency] private AutomationFilterSystem _filter = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedToolSystem _tool = default!;
    [Dependency] protected StartableMachineSystem Machine = default!;
    [Dependency] private SharedCombatModeSystem _combatMode = default!;
    [Dependency] private SharedMeleeWeaponSystem _melee = default!;

    [Dependency] private EntityQuery<ActiveDoAfterComponent> _doAfterQuery = default!;
    [Dependency] private EntityQuery<HandsComponent> _handsQuery = default!;
    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;
    [Dependency] private EntityQuery<ThrownItemComponent> _thrownQuery = default!;

    private readonly HashSet<Entity<PhysicsComponent>> _targets = new();

    public static readonly SpriteSpecifier VerbIcon = new SpriteSpecifier.Rsi(new("Objects/Tools/screwdriver.rsi"), "screwdriver-map");
    public static readonly ProtoId<ToolQualityPrototype> Screwing = "Screwing";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InteractorComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<InteractorComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<InteractorComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<InteractorComponent, DoAfterEndedEvent>(OnDoAfterEnded);
        SubscribeLocalEvent<InteractorComponent, SignalReceivedEvent>(OnSignalReceived);
        // hand visuals
        SubscribeLocalEvent<InteractorComponent, EntInsertedIntoContainerMessage>(OnItemModified);
        SubscribeLocalEvent<InteractorComponent, EntRemovedFromContainerMessage>(OnItemModified);
    }

    private void OnInit(Entity<InteractorComponent> ent, ref ComponentInit args)
    {
        UpdateAppearance(ent);
    }

    private void OnExamined(Entity<InteractorComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(_filter.GetSlot(ent) is {} filter
            ? Loc.GetString("robotic-arm-examine-filter", ("filter", filter))
            : Loc.GetString("robotic-arm-examine-no-filter"));
    }

    private void OnGetVerbs(Entity<InteractorComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract)
            return;

        // need to use a screwdriver to adjust the settings (or a multitool+signaller)
        var noScrewdriver = args.Using is not {} tool || !_tool.HasQuality(tool, Screwing);

        var user = args.User;
        (string, Toggle)[] options = [
            ("alt-interact", () => SetAltInteract(ent, !ent.Comp.AltInteract)),
            ("use-in-hand", () => SetUseInHand(ent, !ent.Comp.UseInHand)),
            ("harm-mode", () => SetHarmMode(ent, !ent.Comp.HarmMode))
        ];
        foreach (var (id, toggle) in options)
        {
            args.Verbs.Add(new()
            {
                Act = () =>
                {
                    var value = toggle();
                    _popup.PopupEntity(Loc.GetString($"interactor-verb-toggled-{id}", ("enabled", value)), ent, user);
                },
                Text = Loc.GetString($"interactor-verb-toggle-{id}"),
                Icon = VerbIcon,
                Disabled = noScrewdriver,
                Message = noScrewdriver ? Loc.GetString("interactor-verb-no-screwdriver") : null
            });
        }
    }

    private void OnItemModified<T>(Entity<InteractorComponent> ent, ref T args) where T: ContainerModifiedMessage
    {
        if (args.Container.ID != ent.Comp.ToolContainerId)
            return;

        UpdateAppearance(ent);
    }

    private void OnDoAfterEnded(Entity<InteractorComponent> ent, ref DoAfterEndedEvent args)
    {
        UpdateToolAppearance(ent);
        if (args.Target is not { } target)
            return;

        if (args.Cancelled)
            Machine.Failed(ent.Owner);
        else
            Machine.Completed(ent.Owner);
    }

    private void OnSignalReceived(Entity<InteractorComponent> ent, ref SignalReceivedEvent args)
    {
        var state = SignalState.Momentary;
        args.Data?.TryGetValue<SignalState>("logic_state", out state);
        bool current;
        if (args.Port == ent.Comp.AltInteractPort)
            current = ent.Comp.AltInteract;
        else if (args.Port == ent.Comp.UseInHandPort)
            current = ent.Comp.UseInHand;
        else if (args.Port == ent.Comp.HarmModePort)
            current = ent.Comp.HarmMode;
        else
            return;

        var value = state switch
        {
            SignalState.Momentary => !current,
            SignalState.High => true,
            SignalState.Low => false,
            _ => false
        };

        if (args.Port == ent.Comp.AltInteractPort)
            SetAltInteract(ent, value);
        else if (args.Port == ent.Comp.UseInHandPort)
            SetUseInHand(ent, value);
        else if (args.Port == ent.Comp.HarmModePort)
            SetHarmMode(ent, value);
    }

    public bool IsValidTarget(Entity<InteractorComponent> ent, EntityUid target)
        => !_thrownQuery.HasComp(target) // thrown items move too fast to be "clicked" on...
            && _automation.CanMachineDetect(target) // ignore ghosts ninjas etc
            && _filter.IsAllowed(_filter.GetSlot(ent), target); // ignore non-filtered entities

    protected bool HasDoAfter(EntityUid uid) => _doAfterQuery.HasComp(uid);

    protected bool TryInteractWith(Entity<InteractorComponent> ent, EntityUid? target)
    {
        // ignore target entirely for use in hand.
        if (ent.Comp.UseInHand)
        {
            if (!_hands.TryGetActiveItem(ent.Owner, out var tool))
                return false;

            _interaction.UserInteraction(ent, Transform(tool.Value).Coordinates, tool, ent.Comp.AltInteract);
            return true; // no real idea if a system handled it so just hope it did
        }

        return target is {} uid && InteractWith(ent, uid);
    }

    protected bool InteractWith(Entity<InteractorComponent> ent, EntityUid target)
    {
        // alt interaction checks for held items via verbs system, just defer to it
        if (ent.Comp.AltInteract)
            return _interaction.AltInteract(ent, target);

        if (!_hands.TryGetActiveItem(ent.Owner, out var tool))
            return _interaction.InteractHand(ent, target);

        if (!ent.Comp.HarmMode)
        {
            var coords = Transform(target).Coordinates;
            return _interaction.InteractUsing(ent, tool.Value, target, coords);
        }

        // instead of interacting via the SharedInteractionSystem, attack the target with the held item
        if (!TryComp<MeleeWeaponComponent>(tool, out var meleeWeapon))
            return false;

        // I turn on combat mode manually for the entity because otherwise the melee attack will fail
        var prev = _combatMode.IsInCombatMode(ent.Owner);
        _combatMode.SetInCombatMode(ent.Owner, true);
        var result = _melee.AttemptLightAttack(ent.Owner, tool.Value, meleeWeapon, target);
        _combatMode.SetInCombatMode(ent.Owner, prev);
        return result;
    }

    protected void UpdateAppearance(EntityUid uid)
    {
        if (HasDoAfter(uid))
            UpdateAppearance(uid, InteractorState.Active);
        else
            UpdateToolAppearance(uid);
    }

    private void UpdateToolAppearance(EntityUid uid)
    {
        var state = _hands.ActiveHandIsEmpty(uid) == false
            ? InteractorState.Inactive
            : InteractorState.Empty;
        UpdateAppearance(uid, state);
    }

    protected void UpdateAppearance(EntityUid uid, InteractorState state) =>
        _appearance.SetData(uid, InteractorVisuals.State, state);

    /// <summary>
    /// Set <see cref="InteractorComponent.AltInteract"> and dirty it.
    /// </summary>
    public bool SetAltInteract(Entity<InteractorComponent> ent, bool alt)
    {
        if (ent.Comp.AltInteract == alt)
            return alt;

        ent.Comp.AltInteract = alt;
        Dirty(ent);
        return alt;
    }

    /// <summary>
    /// Set <see cref="InteractorComponent.UseInHand"> and dirty it.
    /// </summary>
    public bool SetUseInHand(Entity<InteractorComponent> ent, bool use)
    {
        if (ent.Comp.UseInHand == use)
            return use;

        ent.Comp.UseInHand = use;
        Dirty(ent);
        return use;
    }

    /// <summary>
    /// Set <see cref="InteractorComponent.HarmMode"> and dirty it.
    /// </summary>
    public bool SetHarmMode(Entity<InteractorComponent> ent, bool use)
    {
        if (ent.Comp.HarmMode == use)
            return use;

        ent.Comp.HarmMode = use;
        Dirty(ent);
        return use;
    }

    public EntityCoordinates TargetsPosition(EntityUid uid)
    {
        var xform = Transform(uid);
        var offset = (xform.LocalRotation - Angle.FromDegrees(90)).ToVec();
        return xform.Coordinates.Offset(offset);
    }

    /// <summary>
    /// Find the first valid target infront of the interactor.
    /// </summary>
    public EntityUid? FindTarget(Entity<InteractorComponent> ent)
    {
        if (Transform(ent).GridUid is not {} gridUid || !_gridQuery.TryComp(gridUid, out var grid))
            return null;

        var coords = TargetsPosition(ent);
        var tile = _map.CoordinatesToTile(gridUid, grid, coords);

        _targets.Clear();
        _lookup.GetLocalEntitiesIntersecting(gridUid, tile, _targets, flags: LookupFlags.Uncontained);
        foreach (var target in _targets)
        {
            if (IsValidTarget(ent, target))
                return target;
        }
        return null;
    }
}
