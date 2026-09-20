// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Funeral.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Stacks;
using Content.Shared.Whitelist;
using Content.Trauma.Common.Knowledge.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mind.Components;
using Content.Shared.Humanoid;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Funeral.Systems;

/// <summary>
/// Handles marking corpses for funerals with a funeral tool.
/// </summary>
public sealed partial class FuneralAddSystem : EntitySystem
{
    [Dependency] private CommonKnowledgeSystem _knowledge = default!;
    [Dependency] private FuneralAddSystem _enchanting = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStackSystem _stack = default!;
    [Dependency] private MobStateSystem _mobState = default!;

    private List<EntProtoId<FuneralHolyComponent>> _pool = new();

    // probably shitcode, I'm an amateur.
    [SubscribeLocalEvent]
    private void OnBeforeInteract(Entity<FuneralToolComponent> ent, ref BeforeRangedInteractEvent args)
    {
        var user = args.User;
        var target = args.Target;
        if (!args.CanReach || target == args.User || target is not { } item)
            return;

        args.Handled = true;

        // check target is dead and humanoid
        if (args.Target == null || !HasComp<MobStateComponent>(args.Target.Value) || !HasComp<HumanoidProfileComponent>(args.Target.Value)
        || !_mobState.IsDead(args.Target.Value))
            return;

        // check tool whitelist
        if (_whitelist.IsWhitelistFail(ent.Comp.UserWhitelist, user))
        {
            _popup.PopupEntity(Loc.GetString("You aren't qualified to perform funerals."), user, user, PopupType.MediumCaution);
            return;
        }

        // check target's soul moved on
        if (TryComp<MindContainerComponent>(target, out var mind) && mind.HasMind)
        {
            _popup.PopupEntity(Loc.GetString("Can't perform a funeral because the soul is still present."), user, user, PopupType.MediumCaution);
            return;
        }

        var comp = EnsureComp<FuneralHolyComponent>(target.Value);
        _popup.PopupEntity(Loc.GetString("yippie day!"), user, user, PopupType.MediumCaution);
    }
}
// add cube restriction, add inspect details to comp, add embalming criteria maybe,
// come up with a fix for reviving a corpse that had a funeral.
