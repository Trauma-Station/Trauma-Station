// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.MobClass;

/// <summary>
/// Public Api for mob classes. Also, handles BUI events.
/// TODO: if this gets more complex than a simple specialization, support changing classes
/// </summary>
public sealed partial class MobClassSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _admin = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private EntityQuery<MobClassComponent> _mobClassQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActionMobClassComponent, OpenClassSelectorUiEvent>(OnOpenSelector);

        SubscribeLocalEvent<ActionMobClassComponent, MobClassSelectedMessage>(OnClassSelected);
    }

    private void OnOpenSelector(Entity<ActionMobClassComponent> ent, ref OpenClassSelectorUiEvent args)
    {
        var action = ent.Owner;
        var user = args.Performer;

        if (!_mobClassQuery.TryGetComponent(user, out var mobClass) || !ProtoMan.Resolve(mobClass.BelongsTo, out _))
            return;

        _ui.SetUiState(action, MobClassUiKey.Key, new MobClassState(mobClass.BelongsTo));
        _ui.TryToggleUi(action, MobClassUiKey.Key, user);
    }

    private void OnClassSelected(Entity<ActionMobClassComponent> ent, ref MobClassSelectedMessage args)
    {
        if (_actions.GetAction(ent.Owner) is not { } action
            || action.Comp.AttachedEntity is not { } attachedEnt)
            return;

        _ui.CloseUi(action.Owner, MobClassUiKey.Key);

        SelectClass(attachedEnt, args.ClassProto);

        if (ent.Comp.RemoveOnSelected)
            _actions.RemoveAction(attachedEnt, ent.Owner);
    }

    /// <summary>
    /// Selects a class to specialize in. Runs effect after selecting the class
    /// </summary>
    public void SelectClass(Entity<MobClassComponent?> ent, ProtoId<MobClassPrototype> classProto)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false) || ent.Comp.CurrentClass == classProto)
            return;

        // The class must match the mob class group we belong to, otherwise we can't specialize in it.
        if (!ProtoMan.Resolve(ent.Comp.BelongsTo, out var mobGroup) || !mobGroup.Classes.Contains(classProto))
            return;

        if (!ProtoMan.Resolve(classProto, out var mobClass))
            return;

        ent.Comp.CurrentClass = classProto;
        Dirty(ent);

        _effects.ApplyEffects(ent.Owner, mobClass.Effects);

        var ev = new MobClassSelectedEvent();
        RaiseLocalEvent(ent.Owner, ref ev);

        _admin.Add(LogType.MobClass, LogImpact.High, $"User {ent.Owner} has gained the class {classProto} which belongs to {mobGroup}");
    }

    /// <summary>
    /// Gets the current selected class. Returns null if we don't have any.
    /// </summary>
    public ProtoId<MobClassPrototype>? GetClass(Entity<MobClassComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return null;

        return ent.Comp.CurrentClass;
    }

    /// <summary>
    /// Gets the name of the class the entity currently belongs to.
    /// Returns "None" if no class has been selected.
    /// </summary>
    public string GetClassName(Entity<MobClassComponent?> ent)
    {
        if (!_mobClassQuery.Resolve(ent.Owner, ref ent.Comp) || !ProtoMan.Resolve(ent.Comp.CurrentClass, out var proto))
            return "None";

        return proto.Name;
    }
}

/// <summary>
/// Raised on the entity when a mob class has been selected.
/// </summary>
[ByRefEvent]
public record struct MobClassSelectedEvent;
