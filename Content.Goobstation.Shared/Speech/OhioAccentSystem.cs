// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Speech;
using Content.Shared.Random.Helpers;
using Content.Shared.Speech;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.Speech.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Text;

namespace Content.Goobstation.Server.Speech;

public sealed partial class OhioAccentSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ReplacementAccentSystem _replacement = default!;

    private static readonly ProtoId<ReplacementAccentPrototype> Accent = "ohio";

    private readonly StringBuilder _sb = new();

    [SubscribeLocalEvent]
    private void OnAccent(Entity<OhioAccentComponent> ent, ref AccentGetEvent args)
    {
        _sb.Clear();

        var message = _replacement.ApplyReplacements(args.Message, "ohio");
        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));

        // Prefix
        if (rand.Prob(0.15f))
        {
            var pick = rand.Next(1, 3);
            _sb.Append(Loc.GetString($"accent-ohio-prefix-{pick}"));
            _sb.Append(' ');
            _sb.Append(char.ToLowerInvariant(message[0]));
        }
        else
        {
            _sb.Append(char.ToUpperInvariant(message[0]));
        }

        _sb.Append(message, 1, message.Length - 1);

        // Suffixes
        if (rand.Prob(0.3f))
        {
            var pick = rand.Next(1, 8);
            _sb.Append(Loc.GetString($"accent-ohio-suffix-{pick}"));
        }

        args.Message = _sb.ToString();
    }
};
