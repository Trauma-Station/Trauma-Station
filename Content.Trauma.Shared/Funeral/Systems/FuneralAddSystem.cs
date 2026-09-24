// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Funeral.Components;
using Content.Trauma.Common.Chemistry;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mind.Components;
using Content.Shared.Humanoid;
using Content.Shared.Examine;
using Content.Shared.Body;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using System.Runtime.CompilerServices;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Dependency = Robust.Shared.IoC.DependencyAttribute;

namespace Content.Trauma.Shared.Funeral.Systems;

/// <summary>
/// Handles marking corpses for funerals with a funeral tool.
/// </summary>
public sealed partial class FuneralAddSystem : EntitySystem
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    private static readonly ProtoId<OrganCategoryPrototype> Head = "Head";

    // probably shitcode, I'm an amateur.
    [SubscribeLocalEvent]
    private void OnBeforeInteract(Entity<FuneralToolComponent> ent, ref BeforeRangedInteractEvent args)
    {
        var user = args.User;
        var target = args.Target;
        if (!args.CanReach || target == user || target is not { })
            return;

        args.Handled = true;

        // check target is dead and humanoid and not already marked
        if (target == null || !HasComp<MobStateComponent>(target.Value) || !HasComp<HumanoidProfileComponent>(target.Value) || HasComp<FuneralHolyComponent>(target.Value)
        || !_mobState.IsDead(target.Value))
            return;

        // check tool whitelist
        if (_whitelist.IsWhitelistFail(ent.Comp.UserWhitelist, user))
        {
            _popup.PopupEntity(Loc.GetString("funeral-tool-no-whitelist"), user, user, PopupType.SmallCaution);
            return;
        }

        // ensure head exists
        if (_body.GetOrgan(target.Value, Head) is null)
        {
            _popup.PopupEntity(Loc.GetString("funeral-tool-fail-head"), user, user, PopupType.SmallCaution);
            return;
        }

        // check target was not made via cube
        if (TryComp<CubeBornComponent>(target, out _))
        {
            _popup.PopupEntity(Loc.GetString("funeral-tool-fail-cube"), user, user, PopupType.SmallCaution);
            return;
        }

        // check target's soul moved on
        if (TryComp<MindContainerComponent>(target, out var mind) && mind.HasMind)
        {
            _popup.PopupEntity(Loc.GetString("funeral-tool-fail-soul"), user, user, PopupType.SmallCaution);
            return;
        }

        // check for embalming
        if (!_solution.TryGetSolution(target.Value, "bloodstream", out _, out var solution) ||
            !solution.TryGetReagentQuantity(new ReagentId("Formaldehyde", null), out var qty) ||
            qty <= 10)
        {
            _popup.PopupEntity(Loc.GetString("funeral-tool-formaldehyde"), user, user, PopupType.Small);
            return;
        }

        // fancy schmancy
        PredictedSpawnAtPosition(ent.Comp.EffectProto, Transform(target.Value).Coordinates);
        _audio.PlayPredicted(ent.Comp.SoundPath, target.Value, user, AudioParams.Default.WithVolume(-4f));

        // add holy component
        EnsureComp<FuneralHolyComponent>(target.Value);
        _popup.PopupEntity(Loc.GetString("funeral-tool-complete"), user, user, PopupType.Medium);
    }

    // inspect details for holy comp
    [SubscribeLocalEvent]
    private void OnExamined(EntityUid uid, FuneralHolyComponent comp, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("funeral-examine-holy"));
    }
}
// add embalming criteria,
// come up with a fix for reviving a corpse that had a funeral maybe.
