// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Linq;
using Content.Medical.Common.Targeting;
using Content.Medical.Shared.Body;
using Content.Medical.Shared.Consciousness;
using Content.Medical.Shared.Pain;
using Content.Medical.Shared.Traumas;
using Content.Medical.Shared.Wounds;
using Content.Shared.Body;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Medical.Shared.Tourniquet;

/// <summary>
/// This handles tourniqueting people
/// </summary>
public sealed class TourniquetSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly BodyBloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly BodyPartSystem _part = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly PainSystem _pain = default!;
    [Dependency] private readonly WoundSystem _wound = default!;

    private const string TourniquetContainerId = "Tourniquet";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TourniquetComponent, UseInHandEvent>(OnTourniquetUse);
        SubscribeLocalEvent<TourniquetComponent, AfterInteractEvent>(OnTourniquetAfterInteract);

        SubscribeLocalEvent<BodyComponent, TourniquetDoAfterEvent>(OnBodyDoAfter);
        SubscribeLocalEvent<BodyComponent, RemoveTourniquetDoAfterEvent>(OnTourniquetTakenOff);

        SubscribeLocalEvent<BodyComponent, GetVerbsEvent<InnateVerb>>(OnBodyGetVerbs);
    }

    private bool TryTourniquet(EntityUid target, EntityUid user, EntityUid tourniquetEnt, TourniquetComponent tourniquet)
    {
        if (!TryComp<TargetingComponent>(user, out var targeting)
            || !HasComp<BodyComponent>(user)
            || !HasComp<ConsciousnessComponent>(user)) // To prevent people from tourniqueting simple mobs
            return false;

        var (partType, _) = _body.ConvertTargetBodyPart(targeting.Target);
        if (tourniquet.BlockedBodyParts.Contains(partType))
        {
            _popup.PopupClient(Loc.GetString("cant-put-tourniquet-here"), target, user, PopupType.MediumCaution);
            return false;
        }

        var userIdent = Identity.Entity(user, EntityManager);
        _popup.PopupPredicted(Loc.GetString("puts-on-a-tourniquet", ("user", userIdent), ("part", partType)), target, user, PopupType.Medium);
        _audio.PlayPredicted(tourniquet.PutOnSound, target, user, AudioParams.Default.WithVariation(0.125f).WithVolume(1f));

        var doAfterEventArgs =
            new DoAfterArgs(EntityManager,
                user,
                tourniquet.Delay,
                new TourniquetDoAfterEvent(),
                target,
                target: target,
                used: tourniquetEnt)
            {
                BreakOnDamage = true,
                NeedHand = true,
                BreakOnMove = true,
                BreakOnWeightlessMove = false,
            };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
        return true;
    }

    private void TakeOffTourniquet(EntityUid target, EntityUid user, EntityUid tourniquetEnt, TourniquetComponent tourniquet)
    {
        _popup.PopupPredicted(Loc.GetString("takes-off-a-tourniquet",
            ("user", user),
            ("part", tourniquet.BodyPartTorniqueted!)),
            target,
            user,
            PopupType.Medium);
        _audio.PlayPredicted(tourniquet.PutOffSound, target, user, AudioParams.Default.WithVariation(0.125f).WithVolume(1f));

        var doAfterEventArgs =
            new DoAfterArgs(EntityManager, user, tourniquet.RemoveDelay, new RemoveTourniquetDoAfterEvent(), target, target: target, used: tourniquetEnt)
            {
                BreakOnDamage = true,
                NeedHand = true,
                BreakOnMove = true,
                BreakOnWeightlessMove = false,
            };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
    }

    private void OnTourniquetUse(Entity<TourniquetComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (TryTourniquet(args.User, args.User, ent, ent))
            args.Handled = true;
    }

    private void OnTourniquetAfterInteract(Entity<TourniquetComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled
            || !args.CanReach
            || args.Target is not {} target)
            return;

        if (TryTourniquet(target, args.User, ent, ent))
            args.Handled = true;
    }

    private void OnBodyDoAfter(EntityUid ent, BodyComponent comp, ref TourniquetDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is not {} target ||
            args.Used is not {} used || !TryComp<TourniquetComponent>(used, out var tourniquet) ||
            !TryComp<TargetingComponent>(args.User, out var targeting))
            return;

        var container = _container.EnsureContainer<ContainerSlot>(target, TourniquetContainerId);
        if (container.ContainedEntity.HasValue)
        {
            _popup.PopupClient(Loc.GetString("already-tourniqueted"), ent, args.User, PopupType.Medium);
            return;
        }

        var (partType, symmetry) = _body.ConvertTargetBodyPart(targeting.Target);

        // if the target part exists put the tourniquet on it
        if (_part.FindBodyPart((ent, comp), partType, symmetry) is {} targetPart)
        {
            if (!_container.Insert(used, container))
            {
                _popup.PopupClient(Loc.GetString("cant-tourniquet"), ent, args.User, PopupType.Medium);
                return;
            }
            _pain.TryAddPainFeelsModifier(used, "Tourniquet", targetPart, -10f);
            _bloodstream.TryAddBleedModifier(targetPart, "TourniquetPresent", 100, false, true);

            foreach (var woundable in _wound.GetAllWoundableChildren(targetPart))
            {
                _pain.TryAddPainFeelsModifier(used, "Tourniquet", woundable, -10f);
                _bloodstream.TryAddBleedModifier(woundable, "TourniquetPresent", 100, false, true, woundable);
            }

            tourniquet.BodyPartTorniqueted = targetPart;
            return;
        }

        /* TODO NUBODY: reimplement this, nobody uses tourniquets anyway so i dont care
        var tourniquetable = EntityUid.Invalid;
        foreach (var bodyPart in _body.GetBodyChildren(ent, comp))
        {
            if (!bodyPart.Component.Children
                    .Any(bodyPartSlot =>
                        bodyPartSlot.Value.Type == partType && bodyPartSlot.Value.Symmetry == symmetry))
                continue;

            tourniquetable = bodyPart.Id;
            break;
        }

        if (tourniquetable == EntityUid.Invalid)
        {
            _popup.PopupClient(Loc.GetString("missing-body-part"), ent, args.User, PopupType.MediumCaution);
            return;
        }

        var tourniquetableWounds = new List<Entity<WoundComponent, TourniquetableComponent>>();

        foreach (var woundEnt in _wound.GetWoundableWounds(tourniquetable))
        {
            if (!TryComp<TourniquetableComponent>(woundEnt, out var tourniquetableComp))
                continue;

            if (tourniquetableComp.SeveredSymmetry == symmetry && tourniquetableComp.SeveredPartType == partType)
                tourniquetableWounds.Add((woundEnt.Owner, woundEnt.Comp, tourniquetableComp));
        }

        if (tourniquetableWounds.Count <= 0
           || !_container.Insert(used, container))
        {
            _popup.PopupClient(Loc.GetString("no-wounds-tourniquet"), ent, args.User, PopupType.Medium);
            return;
        }

        foreach (var woundEnt in tourniquetableWounds)
        {
            if (!TryComp<BleedInflicterComponent>(woundEnt, out var bleedInflicter))
                continue;

            _bloodstream.TryAddBleedModifier(woundEnt, "TourniquetPresent", 100, false, bleedInflicter);
            woundEnt.Comp2.CurrentTourniquetEntity = args.Used;
        }

        tourniquet.BodyPartTorniqueted = tourniquetable;
        */
    }

    private void OnTourniquetTakenOff(Entity<BodyComponent> ent, ref RemoveTourniquetDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Used is not {} used)
            return;

        if (!TryComp<TourniquetComponent>(used, out var tourniquet))
            return;

        if (!_container.TryGetContainer(ent, TourniquetContainerId, out var container))
            return;

        var tourniquetedBodyPart = tourniquet.BodyPartTorniqueted;
        if (tourniquetedBodyPart == null)
            return;

        var bodyPartComp = Comp<BodyPartComponent>(tourniquetedBodyPart.Value);
        if (tourniquet.BlockedBodyParts.Contains(bodyPartComp.PartType))
        {
            foreach (var woundEnt in _wound.GetWoundableWounds(tourniquetedBodyPart.Value))
            {
                if (!TryComp<BleedInflicterComponent>(woundEnt, out var bleedInflicter))
                    continue;

                if (!TryComp<TourniquetableComponent>(woundEnt, out var tourniquetableComp))
                    continue;

                if (tourniquetableComp.CurrentTourniquetEntity != args.Used)
                    continue;

                tourniquetableComp.CurrentTourniquetEntity = null;
                _bloodstream.TryRemoveBleedModifier(woundEnt, "TourniquetPresent", bleedInflicter);
            }
        }
        else
        {
            _pain.TryRemovePainFeelsModifier(used, "Tourniquet", tourniquetedBodyPart.Value);
            _bloodstream.TryRemoveBleedModifier(tourniquetedBodyPart.Value, "TourniquetPresent", true);

            foreach (var woundable in _wound.GetAllWoundableChildren(tourniquetedBodyPart.Value))
            {
                _pain.TryRemovePainFeelsModifier(used, "Tourniquet", woundable);
                _bloodstream.TryRemoveBleedModifier(woundable, "TourniquetPresent", true, woundable);
            }
        }

        _container.Remove(used, container);

        _hands.TryPickupAnyHand(args.User, used);
        tourniquet.BodyPartTorniqueted = null;

        args.Handled = true;
    }

    private void OnBodyGetVerbs(EntityUid ent, BodyComponent comp, GetVerbsEvent<InnateVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!_container.TryGetContainer(args.Target, TourniquetContainerId, out var container))
            return;

        foreach (var entity in container.ContainedEntities)
        {
            var tourniquet = Comp<TourniquetComponent>(entity);
            InnateVerb verb = new()
            {
                Act = () => TakeOffTourniquet(args.Target, args.User, entity, tourniquet),
                Text = Loc.GetString("take-off-tourniquet", ("part", tourniquet.BodyPartTorniqueted!)),
                // Icon = new SpriteSpecifier.Texture(new ("/Textures/")),
                Priority = 2
            };
            args.Verbs.Add(verb);
        }
    }
}
