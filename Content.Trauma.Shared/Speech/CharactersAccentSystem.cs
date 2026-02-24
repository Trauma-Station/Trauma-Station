// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Speech;
using Robust.Shared.Random;
using System.Text;

namespace Content.Trauma.Shared.Speech;

public sealed class CharactersAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    private StringBuilder _builder = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CharactersAccentComponent, AccentGetEvent>(OnAccentGet);
    }

    private void OnAccentGet(Entity<CharactersAccentComponent> ent, ref AccentGetEvent args)
    {
        _builder.Clear();
        var chars = ent.Comp.Chars;
        foreach (var c in args.Message)
        {
            if (!chars.TryGetValue(c, out var replacements))
            {
                _builder.Append(c);
                continue;
            }

            _builder.Append(_random.Pick(replacements));
        }
        args.Message = _builder.ToString();
    }
}
