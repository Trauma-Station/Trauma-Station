// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Language;
using Content.Trauma.Common.Language.Systems;
using Robust.Shared.Random;
using System.Text;

namespace Content.Trauma.Shared.Language;

/// <summary>
/// Codespeak language's unique obfuscation method.
/// </summary>
public sealed partial class CodespeakObfuscation : ObfuscationMethod
{
    public override void Obfuscate(StringBuilder builder, string message, CommonLanguageSystem context, float ratio)
    {
        var rand = new RobustRandom();
        rand.SetSeed(message.GetHashCode());

        var entMan = IoCManager.Resolve<IEntityManager>();
        var codespeak = entMan.System<CodespeakSystem>();
        var target = message.Length;
        var people = codespeak.GetAllPeople();

        if (context.PseudoRandomNumber(message.GetHashCode() + 1337, 0, 1000) >= (ratio * 1000))
        {
            builder.Append(message);
            return;
        }

        while (builder.Length < target)
        {
            codespeak.GenerateCodePhrase(builder, rand, people);
        }
        // TODO: copy last punctuation char
    }
}
