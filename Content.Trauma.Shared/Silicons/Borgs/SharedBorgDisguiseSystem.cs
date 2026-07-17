// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Actions;
using Content.Trauma.Shared.Silicons.Borgs.Components;

namespace Content.Trauma.Shared.Silicons.Borgs;

/// <summary>
/// Manages Borg disguises, such as the Syndicate Saboteur's chameleon projector.
/// </summary>
public abstract partial class SharedBorgDisguiseSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actionsSystem = default!;
    [Dependency] private MetaDataSystem _meta = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgDisguiseComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BorgDisguiseComponent, ComponentShutdown>(OnCompRemove);
        SubscribeLocalEvent<BorgDisguiseComponent, GetAccessReaderDisplayEvent>(OnGetAccessReaderDisplay);

    }

    private void OnGetAccessReaderDisplay(EntityUid uid, BorgDisguiseComponent comp, ref GetAccessReaderDisplayEvent args)
    {
        if (!comp.Disguised)
            return;

        if (!ProtoMan.TryIndex(comp.DisguisedPrototype, out var disguisedProto))
            return;

        if (!disguisedProto.TryGetComponent<AccessReaderComponent>("AccessReader", out var disguisedAccessReader))
            return;

        args.OverrideAccessLists = disguisedAccessReader.AccessLists;
    }

    /// <summary>
    /// Swaps the shared parts of the entity's components based on the disguise state.
    /// </summary>
    /// <param name="uid">The entity to swap</param>
    /// <param name="comp">The component to use for getting the disguise state and description.</param>
    protected void UpdateSharedAppearance(EntityUid uid, BorgDisguiseComponent comp)
    {
        if (!TryPrototype(uid, out var entityPrototype))
            return;

        if (comp.Disguised && _prototypeManager.TryIndex(comp.DisguisedPrototype, out var disguisedPrototype))
        {
            _meta.SetEntityName(uid, disguisedPrototype.Name);
            _meta.SetEntityDescription(uid, disguisedPrototype.Description);
        }
        else
        {
            _meta.SetEntityName(uid, entityPrototype.Name);
            _meta.SetEntityDescription(uid, entityPrototype.Description);
        }
    }

    #region ActionManagement

    /// <summary>
    /// Gives the action to disguise
    /// </summary>
    private void OnMapInit(EntityUid uid, BorgDisguiseComponent comp, MapInitEvent args)
    {
        _actionsSystem.AddAction(uid, ref comp.ActionEntity, comp.Action);
    }

    /// <summary>
    /// Takes away the action to disguise from the entity.
    /// </summary>
    private void OnCompRemove(EntityUid uid, BorgDisguiseComponent comp, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, comp.ActionEntity);
    }

    #endregion
}

/// <summary>
/// Should be relayed upon using the action.
/// </summary>
public sealed partial class BorgDisguiseToggleActionEvent : InstantActionEvent;
