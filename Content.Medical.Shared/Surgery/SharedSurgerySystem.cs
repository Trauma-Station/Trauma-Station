// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Body;
using Content.Medical.Common.Surgery;
using Content.Medical.Common.Surgery.Tools;
using Content.Medical.Common.Targeting;
using Content.Medical.Shared.Body;
using Content.Medical.Shared.Surgery.Components;
using Content.Medical.Shared.Surgery.Tools;
using Content.Medical.Shared.Wounds;
using Content.Shared.Body;
using Content.Shared.Buckle.Components;
using Content.Shared.DoAfter;
using Content.Shared.EntityEffects;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Standing;
using Content.Trauma.Common.Knowledge.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Medical.Shared.Surgery;

public sealed partial class SharedSurgerySystem : EntitySystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly RotateToFaceSystem _rotateToFace = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;
    [Dependency] private readonly BodyPartSystem _part = default!;
    [Dependency] private readonly CommonKnowledgeSystem _knowledge = default!;
    [Dependency] private readonly WoundSystem _wound = default!;

    private EntityQuery<BodyComponent> _bodyQuery;
    private EntityQuery<StackComponent> _stackQuery;
    private EntityQuery<BodyPartComponent> _partQuery;
    private EntityQuery<SurgeryIgnoreClothingComponent> _ignoreQuery;
    private EntityQuery<SurgeryToolComponent> _toolQuery;

    private static readonly EntProtoId SurgeryKnowledge = "SurgeryKnowledge";

    public override void Initialize()
    {
        base.Initialize();

        _bodyQuery = GetEntityQuery<BodyComponent>();
        _stackQuery = GetEntityQuery<StackComponent>();
        _partQuery = GetEntityQuery<BodyPartComponent>();
        _ignoreQuery = GetEntityQuery<SurgeryIgnoreClothingComponent>();
        _toolQuery = GetEntityQuery<SurgeryToolComponent>();

        SubscribeLocalEvent<SanitizedComponent, SurgerySanitizationEvent>(OnSanitization);
        SubscribeLocalEvent<SanitizedComponent, HeldRelayedEvent<SurgerySanitizationEvent>>(OnHeldSanitization);

        SubscribeLocalEvent<BodyComponent, InteractUsingEvent>(OnInteractSurgery);
        SubscribeLocalEvent<SurgeryTargetComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<SurgeryTargetComponent, InteractEvent>(OnInteract);

        SubscribeLocalEvent<HandsComponent, SurgerySanitizationEvent>(_hands.RefRelayEvent);
        SubscribeLocalEvent<HandsComponent, SurgeryIgnorePreviousStepsEvent>(_hands.RefRelayEvent);

        SubscribeLocalEvent<SurgeryTargetComponent, SurgeryDoAfterEvent>(OnTargetDoAfter);
        SubscribeLocalEvent<BodyComponent, DrapeDoAfterEvent>(OnDrapeDoAfter);
    }

    private void OnInteractSurgery(Entity<BodyComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled) return;

        if (!HasComp<SurgeryDrapesComponent>(args.Used))
            return;

        var ev = new DrapeDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, args.User, TimeSpan.FromSeconds(2), ev, args.Target, args.Used)
        {
            BreakOnMove = true,
            //BreakOnTargetMove = true, I fucking hate wizden dude.
            CancelDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
            NeedHand = true,
            BreakOnHandChange = true,
            AttemptFrequency = AttemptFrequency.EveryTick,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;
    }


    private void OnInteractUsing(Entity<SurgeryTargetComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled) return;

        // TODO: Add more support somewhere?
        var target = TargetBodyPart.Chest;
        if (TryComp<TargetingComponent>(args.User, out var targeting))
            target = targeting.Target;

        var (partType, symmetry) = _body.ConvertTargetBodyPart(target);
        if (_part.FindBodyPart(args.Target, partType, symmetry)?.Owner is not { } part)
            return;

        if (FindSurgery(part, args.Used) is not { } surgeryId)
            return;

        TryDoSurgeryStep(ent.Owner, part, args.User, surgeryId, out _);
        args.Handled = true;
    }

    private void OnInteract(Entity<SurgeryTargetComponent> ent, ref InteractEvent args)
    {
        if (args.Handled) return;
        var target = TargetBodyPart.Chest;
        if (TryComp<TargetingComponent>(args.User, out var targeting))
            target = targeting.Target;

        var (partType, symmetry) = _body.ConvertTargetBodyPart(target);
        if (_part.FindBodyPart(ent.Owner, partType, symmetry)?.Owner is not { } part)
            return;

        if (FindSurgery(ent.Owner, EntityUid.Invalid) is not { } surgeryId)
            return;

        TryDoSurgeryStep(ent.Owner, part, args.User, surgeryId, out _);
        args.Handled = true;
    }

    public ProtoId<SurgeryPrototype>? FindSurgery(EntityUid part, EntityUid tool)
    {
        if (!TryComp<SurgeryToolComponent>(tool, out var toolComp) || toolComp.ActiveSurgicalComp?.GetType() is not { } toolType)
            return null;

        var woundProto = new List<EntProtoId>();
        var wounds = _wound.GetWoundableWounds(part);

        foreach (var wound in wounds)
        {
            if (Prototype(wound.Owner) is { } woundPrototype)
                woundProto.Add(woundPrototype.ID);
        }

        foreach (var surgeryId in _prototypes.EnumeratePrototypes<SurgeryPrototype>())
        {
            if (GetSingleton(surgeryId) is not { } surgery || surgery.Tool is not { })
                continue;


            bool hasTool = false;
            foreach (var (name, registration) in surgery.Tool)
            {
                var requiredType = registration.Component.GetType();

                bool entityHasComponent = HasComp(tool, requiredType);
                bool activeModeMatches = toolComp.ActiveSurgicalComp?.GetType() == requiredType;
                bool isSurgicalTool = typeof(ISurgeryToolComponent).IsAssignableFrom(requiredType);

                if (activeModeMatches || (!isSurgicalTool && entityHasComponent))
                {
                    hasTool = true;
                    break;
                }
            }

            if (!hasTool)
                continue;

            if (surgery.Required is { } required)
            {
                var valid = true;

                foreach (var id in required)
                {
                    if (!woundProto.Contains(id))
                    {
                        valid = false;
                        break;
                    }
                }
                if (!valid)
                    continue;
            }

            if (surgery.Forbidden is { } forbidden)
            {
                var valid = true;
                foreach (var id in forbidden)
                {
                    if (woundProto.Contains(id))
                    {
                        valid = false;
                        break;
                    }
                }
                if (!valid)
                    continue;
            }

            return surgery.ID;
        }
        return null;
    }

    private void OnHeldSanitization(Entity<SanitizedComponent> ent, ref HeldRelayedEvent<SurgerySanitizationEvent> args)
    {
        if (ent.Comp.WorksInHands)
            args.Args.Handled = true;
    }

    private void OnSanitization(Entity<SanitizedComponent> ent, ref SurgerySanitizationEvent args)
    {
        args.Handled = true;
    }

    private void OnTargetDoAfter(Entity<SurgeryTargetComponent> ent, ref SurgeryDoAfterEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (args.Cancelled)
        {
            var failEv = new SurgeryStepFailedEvent(args.User, ent, args.Surgery);
            RaiseLocalEvent(args.User, ref failEv);
            _popup.PopupPredicted(Loc.GetString("surgery-popup-failed"), args.User, args.User, PopupType.SmallCaution);
            return;
        }

        var tool = _hands.GetActiveItemOrSelf(args.User);
        if (args.Handled
            || args.Target is not { } target
            || !IsSurgeryValid(ent, target, args.Surgery, args.User, out var surgery, out var part)
            || !CanPerformSurgery(args.User, ent, part, args.Surgery, tool, false, out _, out _, out _))
        {
            Log.Warning($"{ToPrettyString(args.User)} tried to start invalid surgery.");
            return;
        }

        if (surgery is { } repeat)
        {
            args.Repeat = repeat.Repeat;
            if (surgery?.SurgeryEffects is { } effects)
                _effects.ApplyEffects(part, effects, 1, tool);
        }

        // consume the tool if it's something like using LV cable as stitches
        if (args.ToolUsed)
        {
            if (_stackQuery.TryComp(tool, out var stack))
                _stack.TryUse((tool, stack), 1);
            else
                PredictedQueueDel(tool);
        }


        var userName = Identity.Entity(args.User, EntityManager);
        var targetName = Identity.Entity(ent.Owner, EntityManager);

        _popup.PopupPredicted(Loc.GetString($"surgery-popup-procedure-{surgery?.ID}", ("user", userName), ("target", targetName), ("part", part)), args.User, args.User);
    }

    private void OnDrapeDoAfter(Entity<BodyComponent> ent, ref DrapeDoAfterEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (args.Cancelled)
        {
            _popup.PopupPredicted(Loc.GetString("surgery-popup-drape-failed"), args.User, args.User, PopupType.SmallCaution);
            return;
        }

        if (HasComp<SurgeryTargetComponent>(ent))
        {
            RemComp<SurgeryTargetComponent>(ent);
            _popup.PopupPredicted(Loc.GetString("surgery-popup-primed-no", ("target", Name(ent.Owner))), args.User, args.User);
        }
        else
        {
            AddComp<SurgeryTargetComponent>(ent);
            _popup.PopupPredicted(Loc.GetString("surgery-popup-primed", ("target", Name(ent.Owner))), args.User, args.User);
        }
    }

    /// <summary>
    /// Do a surgery step on a part, if it can be done.
    /// Returns true if it succeeded.
    /// </summary>
    public bool TryDoSurgeryStep(EntityUid body, EntityUid targetPart, EntityUid user, ProtoId<SurgeryPrototype> surgeryProto, out StepInvalidReason error)
    {
        error = StepInvalidReason.None;
        if (!IsSurgeryValid(body, targetPart, surgeryProto, user, out var surgeryNull, out var part) || surgeryNull is not { } surgery)
        {
            error = StepInvalidReason.SurgeryInvalid;
            return false;
        }

        var tool = _hands.GetActiveItemOrSelf(user);
        if (!CanPerformSurgery(user, body, part, surgery, tool, true, out _, out error, out var data))
            return false;

        var toolComp = _toolQuery.CompOrNull(tool);
        var usedEv = new SurgeryToolUsedEvent(user, body);
        usedEv.IgnoreToggle = toolComp?.IgnoreToggle ?? false;
        RaiseLocalEvent(tool, ref usedEv);
        if (usedEv.Cancelled)
        {
            error = StepInvalidReason.ToolInvalid;
            return false;
        }

        if (toolComp?.StartSound is { } sound)
            _audio.PlayPredicted(sound, tool, user);

        _rotateToFace.TryFaceCoordinates(user, _transform.GetMapCoordinates(body).Position);

        // We need to check for nullability because of surgeries that dont require a tool, like Cavity Implants
        var speed = data?.Speed ?? 1f;
        var toolUsed = data?.Used ?? false; // if no tool is being used you can't consume it
        var ev = new SurgeryDoAfterEvent(surgery, toolUsed);
        var duration = GetSurgeryDuration(surgery, user, body, speed);

        var doAfter = new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(duration), ev, body, part)
        {
            BreakOnMove = true,
            //BreakOnTargetMove = true, I fucking hate wizden dude.
            CancelDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
            NeedHand = true,
            BreakOnHandChange = true,
            AttemptFrequency = AttemptFrequency.EveryTick,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
        {
            error = StepInvalidReason.DoAfterFailed;
            return false;
        }

        return true;
    }

    private float GetSurgeryDuration(SurgeryPrototype proto, EntityUid user, EntityUid target, float toolSpeed)
    {
        var speed = toolSpeed;
        if (TryComp<BuckleComponent>(target, out var buckleComp)) // Get buckle component from target.
        {
            if (TryComp<OperatingTableComponent>(buckleComp.BuckledTo, out var operatingTableComponent))  // If they are buckled to entity with operating table component
                speed *= operatingTableComponent.SpeedModifier; // apply surgery speed modifier
            else
                speed /= 1.5f;
        }
        else
        {
            if (_standing.IsDown(target))
                speed /= 2.0f;
            else
                speed *= 4.0f;
        }
        if (user == target)
            speed /= 2.0f;

        if (_knowledge.GetKnowledge(user, SurgeryKnowledge) is { } skill && _knowledge.GetMastery(skill.Comp.NetLevel) >= 5) // Masters are pretty good at this.
            speed *= 3f;

        return proto.Duration / speed;
    }

    public bool IsSurgeryValid(EntityUid body, EntityUid targetPart, ProtoId<SurgeryPrototype> surgeryId, EntityUid user, out SurgeryPrototype? surgery, out EntityUid part)
    {
        part = default;
        surgery = null;

        if (!HasComp<SurgeryTargetComponent>(body) || GetSingleton(surgeryId) is not { } surgeryProto || !TryComp<BodyPartComponent>(targetPart, out var bodyPart) && !_bodyQuery.HasComp(targetPart))
            return false;

        var woundProto = new List<EntProtoId>();
        var wounds = _wound.GetWoundableWounds(targetPart);

        foreach (var wound in wounds)
        {
            if (Prototype(wound.Owner) is { } woundPrototype)
                woundProto.Add(woundPrototype.ID);
        }

        if (surgeryProto.Required is { } required)
        {
            foreach (var id in required)
            {
                if (!woundProto.Contains(id))
                    return false;
            }
        }

        if (surgeryProto.Forbidden is { } forbidden)
        {
            foreach (var id in forbidden)
            {
                if (woundProto.Contains(id))
                    return false;
            }
        }

        part = targetPart;
        surgery = surgeryProto;
        return true;
    }

    private bool CanPerformSurgery(EntityUid user,
       EntityUid body,
       EntityUid part,
       ProtoId<SurgeryPrototype> surgery,
       EntityUid tool,
       bool doPopup,
       out string? popup,
       out StepInvalidReason reason,
       out ISurgeryToolComponent? data)
    {
        data = null;

        var type = _partQuery.CompOrNull(part)?.PartType ?? BodyPartType.Other;

        var slot = type switch
        {
            BodyPartType.Head => SlotFlags.HEAD,
            BodyPartType.Torso => SlotFlags.OUTERCLOTHING | SlotFlags.INNERCLOTHING,
            BodyPartType.Arm => SlotFlags.OUTERCLOTHING | SlotFlags.INNERCLOTHING,
            BodyPartType.Hand => SlotFlags.GLOVES,
            BodyPartType.Leg => SlotFlags.OUTERCLOTHING | SlotFlags.LEGS,
            BodyPartType.Foot => SlotFlags.FEET,
            BodyPartType.Tail => SlotFlags.NONE,
            BodyPartType.Other => SlotFlags.NONE,
            _ => SlotFlags.NONE,
        };

        popup = null;

        CheckArmor(user, body, tool, slot, out popup, out reason);

        if (reason == StepInvalidReason.None)
            return true;

        if (doPopup && popup != null)
            _popup.PopupClient(popup, user, user, PopupType.SmallCaution);

        return false;
    }

    private bool CheckArmor(EntityUid user, EntityUid body, EntityUid tool, SlotFlags slot, out string? popup, out StepInvalidReason invalid)
    {
        if (slot != SlotFlags.NONE && !_ignoreQuery.HasComp(tool) && _inventory.TryGetContainerSlotEnumerator(body, out var containerSlotEnumerator, slot))
        {
            while (containerSlotEnumerator.MoveNext(out var containerSlot))
            {
                if (!containerSlot.ContainedEntity.HasValue)
                    continue;

                invalid = StepInvalidReason.Armor;
                popup = Loc.GetString("surgery-ui-window-steps-error-armor");
                return false;
            }
        }

        popup = null;
        invalid = StepInvalidReason.None;
        return true;
    }

    public SurgeryPrototype? GetSingleton(ProtoId<SurgeryPrototype> surgeryOrStep)
    {
        return _prototypes.Index(surgeryOrStep);
    }
}
