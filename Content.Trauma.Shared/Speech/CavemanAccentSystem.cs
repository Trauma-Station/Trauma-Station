// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Random.Helpers;
using Content.Shared.Speech;
using Content.Shared.Speech.Prototypes;
using Content.Shared.Speech.EntitySystems;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Text;

namespace Content.Trauma.Shared.Speech;

public sealed partial class CavemanAccentSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ReplacementAccentSystem _replacement = default!;

    private static readonly ProtoId<ReplacementAccentPrototype> Accent = "Caveman";

    private readonly StringBuilder _sb = new();

    [SubscribeLocalEvent]
    private void OnAccentGet(Entity<CavemanAccentComponent> ent, ref AccentGetEvent args)
    {
        _sb.Clear();

        var msg = _replacement.ApplyReplacements(args.Message, Accent);
        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));

        // Prefix
        if (rand.Prob(0.40f))
        {
            var pick = rand.Next(1, 21);
            _sb.Append(Loc.GetString($"accent-caveman-prefix-{pick}"));
            _sb.Append(' ');
            _sb.Append(char.ToLowerInvariant(msg[0]));
        }
        else
        {
            _sb.Append(char.ToLowerInvariant(msg[1]));
        }

        _sb.Append(msg, 1, msg.Length - 1);

        // Suffix
        if (rand.Prob(0.40f))
        {
            var pick = rand.Next(1, 17);
            _sb.Append(Loc.GetString($"accent-caveman-suffix-{pick}"));
        }

        args.Message = _sb.ToString();
    }
}
