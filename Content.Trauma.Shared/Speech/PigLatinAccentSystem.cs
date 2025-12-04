// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Speech;
using Robust.Shared.Random;
using System.Linq;
using System.Text;

namespace Content.Trauma.Shared.Speech;

public sealed class PigLatinAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    private static readonly char[] Punctuation = { '!', '?', '.', '-' };
    private static readonly char[] Vowels = { 'a', 'e', 'i', 'o', 'u' };
    private static readonly string[] VowelSuffix = { "yay", "way", "hay" };

    private StringBuilder _builder = new();
    private StringBuilder _punctuation = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PigLatinAccentComponent, AccentGetEvent>(OnAccentGet);
    }

    private void OnAccentGet(Entity<PigLatinAccentComponent> ent, ref AccentGetEvent args)
    {
        args.Message = ChangeMessage(args.Message);
    }

    // basically the same as tg's implementation in text.dm
    public string ChangeMessage(string message)
    {
        message = message.ToLower();

        // first separate punctuation characters
        _builder.Clear();
        _punctuation.Clear();
        foreach (var c in message)
        {
            if (Punctuation.Contains(c))
                _punctuation.Append(c);
            else
                _builder.Append(c);
        }

        // piglatin each word
        message = _builder.ToString();
        _builder.Clear();
        var words = message.Split(' ');
        var end = words.Length - 1;
        for (var i = 0; i <= end; i++)
        {
            var word = words[i];
            AppendWord(word);
            if (i != end)
                _builder.Append(' ');
        }

        // append all the punctuation in the message to the final result
        _builder[0] = char.ToUpper(_builder[0]);
        _builder.Append(_punctuation);
        return _builder.ToString();
    }

    private void AppendWord(string word)
    {
        if (word.Length < 2)
        {
            _builder.Append(word);
            return;
        }

        var first = word[0];
        var second = word[1];
        var firstVowel = Vowels.Contains(first);
        var secondVowel = Vowels.Contains(second);

        // If a word starts with a vowel and a consonant add the word "way" at the end of the word.
        if (firstVowel && !secondVowel)
        {
            _builder.Append(word);
            _builder.Append(_random.Pick(VowelSuffix));
            return;
        }

        // If a word starts with a consonant and a vowel, put the first letter of the word at the end of the word and add "ay."
        if (!firstVowel && secondVowel)
        {
            _builder.Append(word, 1, word.Length - 1);
            _builder.Append(first);
            _builder.Append("ay");
            return;
        }

        // If a word starts with two consonants move the two consonants to the end of the word and add "ay."
        if (!firstVowel && !secondVowel)
        {
            _builder.Append(word, 2, word.Length - 2);
            _builder.Append(first);
            _builder.Append(second);
            _builder.Append("ay");
            return;
        }

        // unmodified (in tg it seems like this was unreachable)
        _builder.Append(word);
    }
}
