// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Common.Humanoid;

public abstract partial class CommonRomanNamingSystem : EntitySystem
{
    /// <summary>
    ///   Generates a random Roman numeral with a length not exceeding 8 characters.
    ///   All possible Roman numerals from 1 to 3,999 can be generated,
    ///   but numbers from 1 to 100 have a higher chance of being rolled.
    /// </summary>
    public abstract string GenerateRomanNumeral();
}
