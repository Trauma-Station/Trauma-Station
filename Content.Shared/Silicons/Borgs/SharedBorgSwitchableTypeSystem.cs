using Content.Shared.Actions;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.Silicons.Borgs;

/// <summary>
/// Implements borg type switching.
/// </summary>
/// <seealso cref="BorgSwitchableTypeComponent"/>
public abstract class SharedBorgSwitchableTypeSystem : EntitySystem
{
    // TODO: Allow borgs to be reset to default configuration.

    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;
    [Dependency] protected readonly IPrototypeManager Prototypes = default!;
    [Dependency] private readonly InteractionPopupSystem _interactionPopup = default!;

    public static readonly EntProtoId ActionId = "ActionSelectBorgType";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgSwitchableTypeComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BorgSwitchableTypeComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<BorgSwitchableTypeComponent, BorgToggleSelectTypeEvent>(OnSelectBorgTypeAction);

        Subs.BuiEvents<BorgSwitchableTypeComponent>(BorgSwitchableTypeUiKey.SelectBorgType,
            sub =>
            {
                sub.Event<BorgSelectTypeMessage>(SelectTypeMessageHandler);
            });
    }

    //
    // UI-adjacent code
    //

    private void OnMapInit(Entity<BorgSwitchableTypeComponent> ent, ref MapInitEvent args)
    {
        _actionsSystem.AddAction(ent, ref ent.Comp.SelectTypeAction, ActionId);
        Dirty(ent);

        // <Goob> - use subtypes
        if (ent.Comp.SelectedBorgType != null &&
            TryComp<BorgSwitchableSubtypeComponent>(ent, out var subtype) &&
            subtype.BorgSubtype is {} sub)
        {
            SelectBorgModule(ent, ent.Comp.SelectedBorgType.Value, sub);
        }
        // </Goob>
    }

    private void OnShutdown(Entity<BorgSwitchableTypeComponent> ent, ref ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(ent.Owner, ent.Comp.SelectTypeAction);
    }

    private void OnSelectBorgTypeAction(Entity<BorgSwitchableTypeComponent> ent, ref BorgToggleSelectTypeEvent args)
    {
        if (args.Handled || !TryComp<ActorComponent>(ent, out var actor))
            return;

        args.Handled = true;

        _userInterface.TryToggleUi((ent.Owner, null), BorgSwitchableTypeUiKey.SelectBorgType, actor.PlayerSession);
    }

    private void SelectTypeMessageHandler(Entity<BorgSwitchableTypeComponent> ent, ref BorgSelectTypeMessage args)
    {
        if (ent.Comp.SelectedBorgType != null)
            return;

        if (!Prototypes.HasIndex(args.Prototype) || !Prototypes.HasIndex(args.Subtype))
            return;

        SelectBorgModule(ent, args.Prototype, args.Subtype);
    }

    //
    // Implementation
    //

    protected virtual void SelectBorgModule(
        Entity<BorgSwitchableTypeComponent> ent,
        ProtoId<BorgTypePrototype> borgType,
        ProtoId<BorgSubtypePrototype> borgSubtype) // Goob
    {
        ent.Comp.SelectedBorgType = borgType;
        // <Goob> set subtype
        if (TryComp<BorgSwitchableSubtypeComponent>(ent, out var subtype))
        {
            subtype.BorgSubtype = borgSubtype;
            Dirty(ent, subtype);
        }
        // </Goob>

        _actionsSystem.RemoveAction(ent.Owner, ent.Comp.SelectTypeAction);
        ent.Comp.SelectTypeAction = null;
        Dirty(ent);

        _userInterface.CloseUi((ent.Owner, null), BorgSwitchableTypeUiKey.SelectBorgType);

        UpdateEntityAppearance(ent);
    }

    protected void UpdateEntityAppearance(Entity<BorgSwitchableTypeComponent> entity)
    {
        // <Goob> - get subtype too
        if (!Prototypes.Resolve(entity.Comp.SelectedBorgType, out var proto) ||
            !TryComp<BorgSwitchableSubtypeComponent>(entity, out var subtype) ||
            !Prototypes.Resolve(subtype.BorgSubtype, out var subtypeProto))
            return;

        UpdateEntityAppearance(entity, proto, subtypeProto);
        // </Goob>
    }

    protected virtual void UpdateEntityAppearance(
        Entity<BorgSwitchableTypeComponent> entity,
        BorgTypePrototype prototype,
        BorgSubtypePrototype subtypePrototype) // Goob
    {
        if (TryComp(entity, out InteractionPopupComponent? popup))
        {
            _interactionPopup.SetInteractSuccessString((entity.Owner, popup), prototype.PetSuccessString);
            _interactionPopup.SetInteractFailureString((entity.Owner, popup), prototype.PetFailureString);
        }

        if (TryComp(entity, out FootstepModifierComponent? footstepModifier))
        {
            footstepModifier.FootstepSoundCollection = prototype.FootstepCollection;
        }

        if (prototype.SpriteBodyMovementState is { } movementState)
        {
            var spriteMovement = EnsureComp<SpriteMovementComponent>(entity);
            spriteMovement.NoMovementLayers.Clear();
            spriteMovement.NoMovementLayers["movement"] = new PrototypeLayerData
            {
                State = prototype.SpriteBodyState,
            };
            spriteMovement.MovementLayers.Clear();
            spriteMovement.MovementLayers["movement"] = new PrototypeLayerData
            {
                State = movementState,
            };
        }
        else
        {
            RemComp<SpriteMovementComponent>(entity);
        }
    }
}
