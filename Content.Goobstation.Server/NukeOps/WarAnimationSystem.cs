// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.SpecialAnimation;
using Content.Server.Audio;
using Content.Server.GameTicking.Rules;
using Content.Server.NukeOps;
using Content.Shared.Audio;
using Content.Shared.NukeOps;
using Robust.Shared.Player;

namespace Content.Goobstation.Server.NukeOps;

public sealed partial class WarAnimationSystem : EntitySystem
{
    [Dependency] private SharedSpecialAnimationSystem _specialAnimation = default!;
    [Dependency] private ServerGlobalSoundSystem _sound = default!;

    [SubscribeLocalEvent(after: [typeof(NukeopsRuleSystem)])] // shitty event as api antipattern
    private void OnWarDeclared(ref WarDeclaredEvent args)
    {
        if (args.Status != WarConditionStatus.WarReady)
            return;

        var ent = args.DeclaratorEntity;
        _specialAnimation.PlayAnimationFiltered(args.User, Filter.Broadcast(), "NukeOpsWarAnimation");
        _sound.DispatchStationEventMusic(ent, ent.Comp.Music, StationEventMusicType.Nuke, ent.Comp.Music.Params);
    }
}
