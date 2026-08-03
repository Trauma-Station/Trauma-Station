// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Speech;
using Content.Shared.Random.Helpers;
using Content.Shared.Speech;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.Speech.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Text;

namespace Content.Goobstation.Shared.Speech;

public sealed partial class MedievalAccentSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ReplacementAccentSystem _replacement = default!;

    private static readonly ProtoId<ReplacementAccentPrototype> Accent = "medieval";

    private readonly StringBuilder _sb = new();

    [SubscribeLocalEvent]
    private void OnAccent(Entity<MedievalAccentComponent> ent, ref AccentGetEvent args)
    {
        _sb.Clear();

        var message = _replacement.ApplyReplacements(args.Message, Accent);
        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));

        // Prefix
        if (rand.Prob(0.40f))
        {
            var pick = rand.Next(1, 42);

            _sb.Append(Loc.GetString($"accent-medieval-prefix-{pick}"));
            _sb.Append(' ');
            // Remove capital in the middle of the new message
            _sb.Append(char.ToLowerInvariant(message[0]));
        }
        else
        {
            _sb.Append(char.ToUpperInvariant(message[0]));
        }

        _sb.Append(message, 1, message.Length - 1);

        args.Message = _sb.ToString();
    }
};
