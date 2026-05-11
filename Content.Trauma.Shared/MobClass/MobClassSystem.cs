// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.MobClass;

/// <summary>
/// Public Api for mob classes. Also, handles BUI events.
/// TODO: if this gets more complex than a simple specialization, support changing classes
/// </summary>
public sealed class MobClassSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly EntityQuery<MobClassComponent> _mobClassQuery = default!;

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

        if (!_mobClassQuery.TryGetComponent(user, out var mobClass) || !_proto.Resolve(mobClass.BelongsTo, out _))
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
        if (!_proto.Resolve(ent.Comp.BelongsTo, out var mobGroup) || !mobGroup.Classes.Contains(classProto))
            return;

        if (!_proto.Resolve(classProto, out var mobClass))
            return;

        ent.Comp.CurrentClass = classProto;
        Dirty(ent);

        _effects.ApplyEffects(ent.Owner, mobClass.Effects);

        var ev = new MobClassSelectedEvent();
        RaiseLocalEvent(ent.Owner, ref ev);
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
}

/// <summary>
/// Raised on the entity when a mob class has been selected.
/// </summary>
[ByRefEvent]
public record struct MobClassSelectedEvent;
