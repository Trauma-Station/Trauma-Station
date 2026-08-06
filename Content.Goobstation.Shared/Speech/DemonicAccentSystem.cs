// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Speech;
using Content.Shared.Random.Helpers;
using Content.Shared.Speech;
using Content.Shared.Speech.EntitySystems;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Text;

namespace Content.Goobstation.Server.Speech;

public sealed partial class DemonicAccentSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ReplacementAccentSystem _replacement = default!;

    private StringBuilder _sb = new();

    [SubscribeLocalEvent]
    private void OnAccentGet(Entity<DemonicAccentComponent> ent, ref AccentGetEvent args)
    {
        _sb.Clear();

        _sb.Append(_replacement.ApplyReplacements(args.Message, "slaughter_demon"));

        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));
        if (rand.Prob(0.15f))
        {
            var pick = rand.Next(1, 8);
            _sb.Append(' ');
            _sb.Append(Loc.GetString($"accent-demonic-suffix-{pick}"));
        }

        args.Message = _sb.ToString().ToUpperInvariant();
    }
}
