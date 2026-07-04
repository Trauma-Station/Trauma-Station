// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Speech.EntitySystems;
using Robust.Shared.Random;
using Content.Shared.Speech;
using Content.Trauma.Server.Speech.Components;

namespace Content.Trauma.Server.Speech.EntitySystems;

public sealed partial class CavemanAccentSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ReplacementAccentSystem _replacement = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CavemanAccentComponent, AccentGetEvent>(OnAccentGet);
    }

    // converts left word when typed into the right word, then wraps with a caveman prefix/suffix
    public string Accentuate(string message, CavemanAccentComponent component)
    {
        var msg = message;

        msg = _replacement.ApplyReplacements(msg, "caveman");

        // Prefix
        if (_random.Prob(0.40f))
        {
            var pick = _random.Next(1, 21);

            msg = msg[0].ToString().ToLower() + msg.Remove(0, 1);
            msg = Loc.GetString($"accent-caveman-prefix-{pick}") + " " + msg;
        }

        // Suffix
        if (_random.Prob(0.40f))
        {
            var pick = _random.Next(1, 17);

            msg += Loc.GetString($"accent-caveman-suffix-{pick}");
        }

        // Sanitize capital again, in case we substituted or prefixed a word that should be capitalized
        msg = msg[0].ToString().ToUpper() + msg.Remove(0, 1);

        return msg;
    }

    private void OnAccentGet(EntityUid uid, CavemanAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, component);
    }
}
