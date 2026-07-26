// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Speech;
using Content.Trauma.Shared.Speech;
using Robust.Shared.Random;

namespace Content.Trauma.Server.Speech;

public sealed partial class VulgarAccentSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;

    public string Accentuate(string message, VulgarAccentComponent comp)
    {
        string[] messageWords = message.Split(" ");

        for (int i = 0; i < messageWords.Length; i++)
        {
            //Every word has a percentage chance to be replaced by a random swear word from the component's array.
            if (_random.Prob(comp.SwearProb))
            {
                if (!ProtoMan.Resolve(comp.Pack, out var messagePack))
                    return message;

                string swearWord = Loc.GetString(_random.Pick(messagePack.Values));
                messageWords[i] = swearWord;
            }
        }

        return string.Join(" ", messageWords);
    }

    [SubscribeLocalEvent]
    private void OnAccentGet(Entity<VulgarAccentComponent> ent, ref AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, ent.Comp);
    }
}
