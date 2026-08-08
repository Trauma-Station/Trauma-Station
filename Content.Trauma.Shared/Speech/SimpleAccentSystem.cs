// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Random.Helpers;
using Content.Shared.Speech;
using Content.Shared.Speech.EntitySystems;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Text;

namespace Content.Trauma.Shared.Speech;

public sealed partial class SimpleAccentSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ReplacementAccentSystem _replacement = default!;

    private readonly StringBuilder _sb = new();

    [SubscribeLocalEvent]
    private void OnAccentGet(Entity<SimpleAccentComponent> ent, ref AccentGetEvent args)
    {
        ApplyAccent(ent.Comp.Accent, ref args);
    }

    public void ApplyAccent([ForbidLiteral] ProtoId<SimpleAccentPrototype> id, ref AccentGetEvent args)
    {
        args.Message = ApplyAccent(id, args.Entity, args.Message);
    }

    public string ApplyAccent([ForbidLiteral] ProtoId<SimpleAccentPrototype> id, EntityUid uid, string msg)
    {
        _sb.Clear();

        var accent = ProtoMan.Index(id);

        // base replacement accent
        if (accent.Replacement is { } replacement)
            msg = _replacement.ApplyReplacements(msg, replacement);

        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(uid));

        // prefix
        var first = msg[0];
        if (accent.Prefix is { } prefixId && rand.Prob(accent.PrefixChance))
        {
            var prefix = rand.Pick(ProtoMan.Index(prefixId));
            _sb.Append(prefix);
            _sb.Append(' ');
            _sb.Append(char.ToLowerInvariant(first));
        }
        else
        {
            _sb.Append(char.ToUpperInvariant(first));
        }

        // the main message
        _sb.Append(msg, 1, msg.Length - 1);

        // suffix
        if (accent.Suffix is { } suffixId && rand.Prob(accent.SuffixChance))
        {
            var suffix = rand.Pick(ProtoMan.Index(suffixId));
            _sb.Append(suffix);
        }

        // make it uppercase if needed
        if (accent.Uppercase)
        {
            for (var i = 0; i < _sb.Length; i++)
            {
                _sb[i] = char.ToUpperInvariant(_sb[i]);
            }
        }

        return _sb.ToString();
    }
}
