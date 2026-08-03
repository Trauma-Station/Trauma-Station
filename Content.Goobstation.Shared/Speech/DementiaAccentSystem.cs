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

public sealed partial class DementiaAccentSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ReplacementAccentSystem _replacement = default!;

    private static readonly ProtoId<ReplacementAccentPrototype> Accent = "dementia";

    private readonly StringBuilder _sb = new();

    [SubscribeLocalEvent]
    private void OnAccent(Entity<DementiaAccentComponent> ent, ref AccentGetEvent args)
    {
        _sb.Clear();

        var message = _replacement.ApplyReplacements(args.Message, Accent);
        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));

        // Prefix
        if (rand.Prob(0.15f))
        {
            var pick = rand.Next(1, 5);

            _sb.Append(Loc.GetString($"accent-dementia-prefix-{pick}"));
            _sb.Append(' ');
            // Remove capital from middle of the new message
            _sb.Append(char.ToLowerInvariant(message[0]));
        }
        else
        {
            // Regular capital for first letter
            _sb.Append(char.ToUpperInvariant(message[0]));
        }

        _sb.Append(message, 1, message.Length - 1);

        // Suffixes
        if (rand.Prob(0.3f))
        {
            var pick = rand.Next(1, 6);
            _sb.Append(Loc.GetString($"accent-dementia-suffix-{pick}"));
        }

        args.Message = _sb.ToString();
    }
};
