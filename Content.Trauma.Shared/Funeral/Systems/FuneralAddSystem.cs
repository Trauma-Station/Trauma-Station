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
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

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

    // probably shitcode, I'm an amateur.
    [SubscribeLocalEvent]
    private void OnBeforeInteract(Entity<FuneralToolComponent> ent, ref BeforeRangedInteractEvent args)
    {
        var user = args.User;
        var target = args.Target;
        if (!args.CanReach || target == args.User || target is not { })
            return;

        args.Handled = true;

        // check target is dead and humanoid and not already marked
        if (args.Target == null || !HasComp<MobStateComponent>(args.Target.Value) || !HasComp<HumanoidProfileComponent>(args.Target.Value) || HasComp<FuneralHolyComponent>(args.Target.Value)
        || !_mobState.IsDead(args.Target.Value))
            return;

        // check tool whitelist
        if (_whitelist.IsWhitelistFail(ent.Comp.UserWhitelist, user))
        {
            _popup.PopupEntity(Loc.GetString("funeral-tool-no-whitelist"), user, user, PopupType.SmallCaution);
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
// add head requirement, add embalming criteria maybe,
// come up with a fix for reviving a corpse that had a funeral maybe.
