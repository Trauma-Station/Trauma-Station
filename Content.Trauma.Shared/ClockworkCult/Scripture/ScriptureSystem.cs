// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;
using Content.Shared.EntityEffects;
using Content.Shared.Popups;
using Content.Shared.Prototypes;
using Content.Trauma.Shared.ClockworkCult.Slab;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.ClockworkCult.Scripture;

/// <summary>
/// This handles the Scripture system.
/// A scripture is a <see cref="EntityPrototype"/> that holds another entity as a "produced result".
/// Scriptures can produce a result by getting recited via an entity with <see cref="ClockworkSlabComponent"/>.
/// A scripture can hold actions, structures, and other entities.
/// </summary>
public sealed partial class ScriptureSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _entityEffects = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityQuery<ScriptureComponent> _scriptureQuery = default!;
    [Dependency] private readonly EntityQuery<ScriptureTierComponent> _scriptureTierQuery = default!;

    /// <summary>
    /// All entity prototypes with <see cref="ScriptureComponent"/>.
    /// </summary>
    [ViewVariables]
    public List<EntProtoId> AllScriptures = new();

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        InitializeCharges();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        SubscribeLocalEvent<ScriptureContainerComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<ScriptureContainerComponent, ComponentShutdown>(OnShutdown);

        Subs.BuiEvents<ScriptureContainerComponent>(ClockworkSlabUiKey.Key, subs =>
        {
            subs.Event<ScriptureReciteMessage>(OnRecite);
        });

        SubscribeLocalEvent<ScriptureTierComponent, BeforeScriptureReciteEvent>(OnBeforeReciteTier);
        SubscribeLocalEvent<DoAfterArgsComponent, BeforeScriptureReciteEvent>(OnBeforeReciteDoAfter);

        SubscribeLocalEvent<ScriptureComponent, ScriptureReciteDoAfterEvent>(OnDoAfterScripture);
        SubscribeLocalEvent<ScriptureTierComponent, ScriptureReciteDoAfterEvent>(OnDoAfterScriptureTier);

        LoadPrototypes();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.WasModified<EntityPrototype>())
            return;

        LoadPrototypes();
    }

    /// <summary>
    ///  Initialize the scripture container on the entity
    /// </summary>
    private void OnCompInit(Entity<ScriptureContainerComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Scriptures = _container.EnsureContainer<Container>(ent, ScriptureContainerComponent.ContainerId);
    }

    /// <summary>
    ///  Clear the scripture container
    /// </summary>
    private void OnShutdown(Entity<ScriptureContainerComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Scriptures is not { } container)
            return;

        _container.ShutdownContainer(container);
    }

    /// <summary>
    /// Handles basic reciting logic.
    /// Checks against the user to see if they hold the correct requirements to recite (e.g. power)
    /// Applies the recital effects on the user, if user is capable of reciting.
    /// </summary>
    private void OnRecite(Entity<ScriptureContainerComponent> ent, ref ScriptureReciteMessage args)
    {
        // TODO: Must marked as a clockwork cultist to recite!!
        if (!_proto.TryIndex(args.Scripture, out var scripture))
            return;

        var user = args.Actor;

        // If scripture does not exist in our container, then we don't continue further
        var scriptureEntity = TryGetScripture(ent.AsNullable(), scripture);
        if (scriptureEntity is not {} scriptureEnt || !_scriptureQuery.TryComp(scriptureEnt, out var scriptureComponent) )
            return;

        // Making sure the user doesn't spam the logic
        var nextAttempt = scriptureComponent.LastTry + scriptureComponent.Delay;
        if (_timing.CurTime < nextAttempt)
        {
            _popup.PopupClient($"Wait {(nextAttempt - _timing.CurTime).Seconds} seconds before trying again.", user, PopupType.MediumCaution);
            return;
        }
        scriptureComponent.LastTry = _timing.CurTime;
        Dirty(scriptureEnt, scriptureComponent);

        // Raised on user to check if we have enough power to cast
        var attemptEv = new ReciteAttemptEvent(scriptureComponent.PowerCost);
        RaiseLocalEvent(user, ref attemptEv);
        if (attemptEv.Cancelled)
            return;

        // Check for other components that may override our behaviour like DoAfterArgs
        var beforeEv = new BeforeScriptureReciteEvent(user, args.TierData);
        RaiseLocalEvent(scriptureEnt, ref beforeEv);
        if (beforeEv.Handled)
            return;

        // We don't have tiers or anything else, just add the normal recital effects of the scripture
        // Note: if you're yamlmaxxing, make sure to not have recital effects on this component, if you have tiers too
        if (scriptureComponent.RecitalEffects is not { } recitalEffects)
            return;

        _entityEffects.ApplyEffects(user, recitalEffects);
    }

    private void OnBeforeReciteTier(Entity<ScriptureTierComponent> ent, ref BeforeScriptureReciteEvent args)
    {
        // Can be handled by doafter
        if (args.Handled)
            return;

        // This part handles scripture tier logic.
        // Iterates over all tiers and checks the data passed in the event,
        // if the tier is valid and is found, then we apply the effects
        if (args.TierId is not { } tierId)
            return;

        CastTierScripture(ent, args.User, tierId);
    }

    private void OnBeforeReciteDoAfter(Entity<DoAfterArgsComponent> ent, ref BeforeScriptureReciteEvent args)
    {
        args.Handled = true;

        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            user: args.User,
            delay: ent.Comp.Delay,
            @event: new ScriptureReciteDoAfterEvent(args.TierId),
            eventTarget: ent.Owner)
        {
            BlockDuplicate = true,
            BreakOnDamage = ent.Comp.BreakOnDamage,
            BreakOnMove = ent.Comp.BreakOnMove,
            BreakOnDropItem = ent.Comp.BreakOnDropItem,
            BreakOnHandChange = ent.Comp.BreakOnHandChange,
            Hidden = ent.Comp.Hidden
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnDoAfterScripture(Entity<ScriptureComponent> ent, ref ScriptureReciteDoAfterEvent args)
    {
        // In case this gets handled by scripture tiers
        if (args.Handled || args.Cancelled)
            return;

        if (ent.Comp.RecitalEffects is not { } recitalEffects)
            return;

        _entityEffects.ApplyEffects(args.User, recitalEffects);
    }

    private void OnDoAfterScriptureTier(Entity<ScriptureTierComponent> ent, ref ScriptureReciteDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        Log.Debug("Is this even getting run? 2");

        if (args.TierId is not { } tierId)
            return;

        CastTierScripture(ent, args.User, tierId);
        args.Handled = true;
    }

    private void LoadPrototypes()
    {
        AllScriptures.Clear();
        var scripture = Factory.GetComponentName<ScriptureComponent>();
        foreach (var proto in _proto.EnumeratePrototypes<EntityPrototype>())
        {
            if (!proto.Components.ContainsKey(scripture))
                continue;

            var id = proto.ID;
            AllScriptures.Add(id);
        }
    }

    #region Public Api

    /// <summary>
    /// Tries to add a scripture to an entity,
    /// ensures <see cref="ScriptureContainerComponent"/> if it doesn't exist on the target.
    /// </summary>
    /// <returns></returns>
    public bool TryAddScripture(EntityUid target, EntProtoId scriptureProto)
    {
        // TODO: Do duplicate scripture check
        if (!CanAddScripture(scriptureProto))
            return false;

        EnsureComp<ScriptureContainerComponent>(target);

        if (!PredictedTrySpawnInContainer(scriptureProto, target, ScriptureContainerComponent.ContainerId, out _))
            return false;

        return true;
    }

    /// <summary>
    /// Gets the scripture prototype from an entity with <see cref="ScriptureComponent"/>
    /// </summary>
    public EntProtoId? TryGetScripturePrototype(EntityUid scripture)
    {
        if (!_scriptureQuery.HasComp(scripture))
            return null;

        return Prototype(scripture)?.ID;
    }

    /// <summary>
    /// Gets a scripture <see cref="EntityUid"/> via its <see cref="EntityPrototype"/>, returns null if not found.
    /// </summary>
    public EntityUid? TryGetScripture(Entity<ScriptureContainerComponent?> ent, EntProtoId scripture)
    {
        if (!Resolve(ent.Owner, ref ent.Comp) || ent.Comp.Scriptures is not {} scriptures)
            return null;

        foreach (var scriptEnt in scriptures.ContainedEntities)
        {
            if (TryGetScripturePrototype(scriptEnt) is not {} scriptProto)
                continue;

            if (scriptProto == scripture)
                return scriptEnt;
        }

        return null;
    }

    /// <summary>
    /// Determines whether we can add a scripture to an entity
    /// </summary>
    /// <param name="scripture"></param> The prototype of the scripture
    /// <returns></returns>
    public bool CanAddScripture(EntProtoId scripture)
    {
        if (!_proto.Resolve(scripture, out var scriptureData))
            return false;

        if (!scriptureData.HasComponent<ScriptureComponent>())
            return false;

        return true;
    }

    /// <summary>
    /// Unlcoks a specific tier in an entity with <see cref="ScriptureTierComponent"/>.
    /// </summary>
    public void UnlockTier(Entity<ScriptureTierComponent?> ent, ScriptureTierData tier)
    {
        if (!_scriptureTierQuery.Resolve(ent.Owner, ref ent.Comp))
            return;

        for (int i = 0; i < ent.Comp.Tiers.Count; i++)
        {
            var currentTier = ent.Comp.Tiers[i];
            if (currentTier.Id != tier.Id)
                continue;

            currentTier.Locked = false;
            ent.Comp.Tiers[i] = currentTier;
            Dirty(ent);
            return;
        }
    }
    #endregion

    #region Helpers

    /// <summary>
    ///  Casts a scripture that has tiers
    /// </summary>
    private void CastTierScripture(Entity<ScriptureTierComponent> ent, EntityUid user, string tierId)
    {
        foreach (var tier in ent.Comp.Tiers)
        {
            if (tier.Id != tierId)
                continue;

            if (tier.Locked)
                continue;

            _entityEffects.ApplyEffects(user, tier.RecitalEffects);
            return;
        }
    }
    #endregion
}
