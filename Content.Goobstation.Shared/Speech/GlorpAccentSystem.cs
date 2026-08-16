// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Speech;
using Content.Shared.Random.Helpers;
using Content.Shared.Speech;
using Content.Shared.Speech.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Text;
using System.Text.RegularExpressions;

namespace Content.Goobstation.Shared.Speech;

public sealed partial class GlorpAccentSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;

    private static readonly string[] StartingLetters = { "n", "x", "z", "v", "g" };
    private static readonly string[] Suffixes = { "narp", "lorp", "leeb", "orp", "orple", "ip", "op", "eegle" };
    private static readonly string[] RandomInserts = { "Glupshitto", "Glorpshit" };
    private static readonly HashSet<string> WhitelistedWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "discrimination", "inferior", "surgery", "probing", "neanderthal", "animal",
        "tool", "heart", "zoo", "subject", "organ", "skill", "issue", "extract", "remove", "eyes",
        "sleep", "bruh", "skibidi", "ohio", "brazil", "shitsec", "silly", "yippee", "bald"
    };
    private static readonly Regex WordRegex = new(@"\b\w+\b", RegexOptions.IgnoreCase);

    private readonly List<string> _words = new();
    private readonly StringBuilder _sb = new();

    private void GenerateRandomAlienWord(IRobustRandom rand)
    {
        _sb.Append(rand.Pick(StartingLetters));
        _sb.Append(rand.Pick(Suffixes));
    }

    private void AdjustCapitalization(string word, bool allCaps)
    {
        if (string.IsNullOrEmpty(word))
            return;

        if (allCaps)
        {
            _sb.Append(word.ToUpperInvariant());
        }
        else
        {
            var i = _sb.Length;
            _sb.Append(char.ToUpperInvariant(word[0]));
            _sb.Append(word, 1, word.Length - 1);
        }
    }

    private void AdjustCapitalization(int offset, int len)
    {
        var end = offset + len;
        for (int i = offset; i < end; i++)
        {
            _sb[i] = char.ToUpperInvariant(_sb[i]);
        }
    }

    private bool IsWhitelisted(string word)
    {
        // whitelist check
        word = word.ToLowerInvariant();
        if (WhitelistedWords.Contains(word))
            return true;

        // plurality check
        if (word.Length > 1 && word.EndsWith("s", StringComparison.OrdinalIgnoreCase))
        {
            var singular = word.Substring(0, word.Length - 1);
            if (WhitelistedWords.Contains(singular))
                return true;
        }

        return false;
    }

    private void ProcessWord(string originalWord, bool allCaps, IRobustRandom rand)
    {
        // whitelist plus plurality
        if (IsWhitelisted(originalWord))
        {
            _sb.Append('"');
            AdjustCapitalization(originalWord, allCaps);
            _sb.Append('"');
            return;
        }

        // if not whitelisted, replace with some real glorp shit
        var offset = _sb.Length;
        GenerateRandomAlienWord(rand);
        var wordLen = _sb.Length - offset;
        AdjustCapitalization(offset, allCaps ? wordLen : 1);
    }

    private string ReplaceWithRandomAlienWords(string message, bool allCaps, IRobustRandom rand)
    {
        _words.Clear();
        var previousWord = string.Empty;

        foreach (Match match in WordRegex.Matches(message))
        {
            var currentWord = match.Value;
            _sb.Clear();
            ProcessWord(currentWord, allCaps, rand);
            var processedWord = _sb.ToString();

            // checks if two whitelisted words are next to eachother
            if (IsWhitelisted(previousWord) && IsWhitelisted(currentWord))
            {
                // combine while quoted
                _sb.Clear();
                _sb.Append('"');
                AdjustCapitalization(previousWord, allCaps);
                _sb.Append(' ');
                AdjustCapitalization(currentWord, allCaps);
                _sb.Append('"');
                _words[_words.Count - 1] = _sb.ToString();
            }
            else
            {
                _words.Add(processedWord);
            }

            previousWord = currentWord;
        }

        // adds glupshitto and glorpshit randomly
        if (rand.Prob(0.25f))
        {
            var randomInsert = rand.Pick(RandomInserts);
            var randomPosition = rand.Next(_words.Count + 1);
            _sb.Clear();
            AdjustCapitalization(randomInsert, allCaps);
            _words.Insert(randomPosition, _sb.ToString());
        }

        return string.Join(" ", _words);
    }

    public string Accentuate(string msg, IRobustRandom rand)
    {
        var allCaps = IsAllCaps(msg);
        return ReplaceWithRandomAlienWords(msg, allCaps, rand);
    }

    private bool IsAllCaps(string message)
    {
        var hasLetters = false;
        foreach (var c in message)
        {
            if (char.IsLetter(c))
            {
                hasLetters = true;
                if (char.IsLower(c))
                    return false;
            }
        }
        return hasLetters;
    }

    [SubscribeLocalEvent]
    private void OnAccentGet(Entity<GlorpAccentComponent> ent, ref AccentGetEvent args)
    {
        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));
        args.Message = Accentuate(args.Message, rand);
    }
}
