// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Bible.Components;
using Content.Shared.Interaction;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Trauma.Shared.Chaplain.ItemBless;


public sealed partial class BlessItem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;

    [SubscribeLocalEvent]
    private void OnInteractUsing(Entity<BlessableComponent> ent, ref InteractUsingEvent args)
    {
        if (!TryComp(args.Used, out BibleComponent? bible)) // TODO: Add optional bible user or magic literacy requirement if anything strong needs this
            return;

        var user = args.User;
        var xform = Transform(ent).Coordinates;

        _audio.PlayPredicted(bible.HealSoundPath, user, user);
        var result = PredictedSpawnAtPosition(ent.Comp.BlessResult, xform); //Spawn what blessing it turns it into
        PredictedQueueDel(ent);
    }
}
