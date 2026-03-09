// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Speech.EntitySystems;
using Content.Shared.Speech;
using Content.Shared.Speech.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Goobstation.Server.Speech;

public sealed class VulgarAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ILocalizationManager _loc = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VulgarAccentComponent, AccentGetEvent>(OnAccentGet);
    }

    public string Accentuate(string message, VulgarAccentComponent component)
    {
        string[] messageWords = message.Split(" ");

        for (int i = 0; i < messageWords.Length; i++)
        {
            //Every word has a percentage chance to be replaced by a random swear word from the component's array.
            if (_random.Prob(component.SwearProb))
            {
                if (!_proto.Resolve(component.Pack, out var messagePack))
                    return message;

                string swearWord = _loc.GetString(_random.Pick(messagePack.Values));
                messageWords[i] = swearWord;
            }
        }

        return string.Join(" ", messageWords);
    }

    public void OnAccentGet(EntityUid uid, VulgarAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, component);
    }
}
