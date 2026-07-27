// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
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
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private MetaDataSystem _meta = default!;
    [Dependency] protected AccessReaderSystem _access = default!;
    [Dependency] protected SharedPointLightSystem _light = default!;

    protected CompName _accessName;

    public override void Initialize()
    {
        base.Initialize();

        _accessName = Factory.CompName<AccessReaderComponent>();
    }

    /// <summary>
    /// When disguised, makes the borg's access reader show the access of whatever it's pretending to be.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnGetAccessReaderDisplay(Entity<BorgDisguiseComponent> ent, ref GetAccessReaderDisplayEvent args)
    {
        if (!ent.Comp.Disguised)
            return;

        if (!TryGetDisguisedPrototype(ent.Comp, out var disguisedProto))
            return;

        if (!disguisedProto.TryComp(_accessName, out AccessReaderComponent? disguisedAccessReader))
            return;

        args.OverrideAccessLists = disguisedAccessReader.AccessLists;
    }

    /// <summary>
    /// Resolves the entity prototype this disguise turns the borg into, if any.
    /// </summary>
    protected bool TryGetDisguisedPrototype(BorgDisguiseComponent comp, [NotNullWhen(true)] out EntityPrototype? disguisedProto)
    {
        return ProtoMan.Resolve(comp.DisguisedPrototype, out disguisedProto);
    }

    /// <summary>
    /// Swaps the shared parts of the entity's components based on the disguise state.
    /// </summary>
    protected void UpdateSharedAppearance(Entity<BorgDisguiseComponent> ent)
    {
        if (!TryPrototype(ent.Owner, out var entityPrototype))
            return;

        if (ent.Comp.Disguised && TryGetDisguisedPrototype(ent.Comp, out var disguisedPrototype))
        {
            _meta.SetEntityName(ent.Owner, disguisedPrototype.Name);
            _meta.SetEntityDescription(ent.Owner, disguisedPrototype.Description);
        }
        else
        {
            _meta.SetEntityName(ent.Owner, entityPrototype.Name);
            _meta.SetEntityDescription(ent.Owner, entityPrototype.Description);
        }
    }

    #region ActionManagement

    /// <summary>
    /// Gives the action to disguise
    /// </summary>
    [SubscribeLocalEvent]
    private void OnMapInit(Entity<BorgDisguiseComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.Action);
    }

    /// <summary>
    /// Takes away the action to disguise from the entity.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnCompRemove(Entity<BorgDisguiseComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }

    #endregion
}

/// <summary>
/// Should be relayed upon using the action.
/// </summary>
public sealed partial class BorgDisguiseToggleActionEvent : InstantActionEvent;
