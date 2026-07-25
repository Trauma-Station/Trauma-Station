// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Random.Helpers;
using Content.Shared.Speech;
using Robust.Shared.Timing;
using Robust.Shared.Random;

namespace Content.Trauma.Shared.Speech;

public sealed partial class VulgarAccentSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;

    [SubscribeLocalEvent]
    private void OnAccentGet(Entity<VulgarAccentComponent> ent, ref AccentGetEvent args)
    {
        if (!ProtoMan.Resolve(ent.Comp.Pack, out var messagePack))
            return;

        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));
        var words = args.Message.Split(" ");
        for (int i = 0; i < words.Length; i++)
        {
            // Every word has a percentage chance to be replaced by a random swear word from the component's array.
            if (rand.Prob(ent.Comp.SwearProb))
            {
                words[i] = Loc.GetString(rand.Pick(messagePack.Values));
            }
        }

        args.Message = string.Join(" ", words);
    }
}
