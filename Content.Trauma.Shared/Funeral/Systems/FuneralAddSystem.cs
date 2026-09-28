// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Content.Trauma.Shared.Funeral.Components;
using Content.Trauma.Shared.Chemistry;
using Robust.Shared.Audio.Systems;

namespace Content.Trauma.Shared.Funeral.Systems;

/// <summary>
/// Handles marking corpses for funerals with a funeral tool.
/// </summary>
public sealed partial class FuneralAddSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;

    private static readonly ProtoId<OrganCategoryPrototype> Head = "Head";
    private static readonly ProtoId<ReagentPrototype> Formaldehyde = "Formaldehyde";

    [SubscribeLocalEvent]
    private void OnBeforeInteract(Entity<FuneralToolComponent> ent, ref BeforeRangedInteractEvent args)
    {
        var user = args.User;
        if (!args.CanReach || args.Target == user || args.Target is not { } target)
            return;

        args.Handled = true;

        // check target is dead and humanoid
        if (!HasComp<MobStateComponent>(target) || !HasComp<HumanoidProfileComponent>(target)
        || !_mobState.IsDead(target))
            return;

        // check tool whitelist
        if (_whitelist.IsWhitelistFail(ent.Comp.UserWhitelist, user))
        {
            _popup.PopupEntity("You aren't qualified to consecrate.", user, user, PopupType.SmallCaution);
            return;
        }

        // check target is not already marked
        if (HasComp<FuneralHolyComponent>(target))
        {
            _popup.PopupEntity("Individual has already been consecrated.", user, user, PopupType.Small);
            return;
        }

        // ensure head exists
        if (_body.GetOrgan(target, Head) is null)
        {
            _popup.PopupEntity("Can't consecrate an individual with no head.", user, user, PopupType.SmallCaution);
            return;
        }

        // check target was not made via cube
        if (HasComp<CubeBornComponent>(target))
        {
            _popup.PopupEntity("Can't consecrate an individual with no history.", user, user, PopupType.SmallCaution);
            return;
        }

        // check target's soul moved on
        if (TryComp<MindContainerComponent>(target, out var mind) && mind.HasMind)
        {
            _popup.PopupEntity("Can't consecrate because the soul is still present.", user, user, PopupType.SmallCaution);
            return;
        }

        // check for embalming
        if (!TryComp<BloodstreamComponent>(target, out var blood) ||
            !_solution.ResolveSolution(target, blood.BloodSolutionName, ref blood.BloodSolution, out var solution) ||
            !solution.TryGetReagentQuantity(new ReagentId(Formaldehyde, null), out var qty) || qty <= 10)
        {
            _popup.PopupEntity("Next, embalm the corpse with 15u formaldehyde.", user, user, PopupType.Small);
            return;
        }

        // fancy schmancy
        PredictedSpawnAtPosition(ent.Comp.EffectProto, Transform(target).Coordinates);
        _audio.PlayPredicted(ent.Comp.SoundPath, target, user);

        // add holy component
        EnsureComp<FuneralHolyComponent>(target);
        _popup.PopupEntity("The consecration has been completed.", user, user, PopupType.Medium);
    }

    // inspect details for holy comp
    [SubscribeLocalEvent]
    private void OnExamined(EntityUid uid, FuneralHolyComponent comp, ExaminedEvent args)
    {
        args.PushMarkup("[color=yellow]This individual has been consecrated and can be cremated for holy ash.[/color]");
    }
}
